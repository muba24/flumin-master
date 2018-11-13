#include "stdafx.h"
#include <vector>
#include <queue>
#include <map>
#include "NILoop.h"

enum ERR_CODE {
    ERR_CODE_SUCCESS = 0,
    ERR_CODE_READ_FAILED = -1
};

enum TASK_TYPE {
    TASK_TYPE_ANALOG_INPUT = 0,
    TASK_TYPE_DIGITAL_INPUT
};

struct handle_info {
    TaskHandle  handle;
    TASK_TYPE   type;
    int         samples_per_chan;
    int         buffer_size;
    HANDLE      mutex_buffers;
    ERR_CODE    result;

    bool const operator == (const handle_info &o) const { return o.handle == handle; }
    //bool const operator <  (const handle_info &o) const { return o.handle < handle;  }
};

struct buffer {
    void* parent;
    void* pData;
    int len;
    int result;
};

buffer buffer_create(void* parent, int size) {
    buffer result;
    result.pData = new char[size];
    result.parent = parent;
    result.len = size;
    return result;
}

void buffer_destroy(buffer b) {
    delete [] b.pData;
}

struct poll_thread_data {
    volatile bool stop;
    std::map<TaskHandle, std::queue<buffer>> free_buffers;
    std::map<TaskHandle, std::queue<buffer>> read_buffers;
    std::vector<handle_info> handles;
    HANDLE hThread;
    HANDLE hThreadEndEvent;
};

DWORD WINAPI PollThread(LPVOID pdata);

NILOOP_API poll_thread_data* start_polling(handle_info* handles, int handleCount) {
    poll_thread_data* data = new poll_thread_data();

    for (int i = 0; i < handleCount; i++) {
        data->handles.push_back(handles[i]);
        data->handles.back().mutex_buffers = CreateMutex(NULL, FALSE, NULL);
        std::queue<buffer> queue;
        for (int j = 0; j < 5; j++) {
            queue.emplace(buffer_create(data, handles[i].buffer_size * ((handles[i].type == TASK_TYPE::TASK_TYPE_ANALOG_INPUT) ? 8 : 4)));
        }
        data->free_buffers.emplace(handles[i].handle, queue);
    }

    data->stop = false;
    data->hThreadEndEvent = CreateEvent(NULL, TRUE, FALSE, NULL);

    data->hThread = CreateThread(NULL, 0, PollThread, (LPVOID)data, 0, NULL);
    if (data->hThread == NULL) return NULL;

    return data;
}

NILOOP_API void stop_polling(poll_thread_data* data) {
    data->stop = true;
    WaitForSingleObject(data->hThreadEndEvent, INFINITE);

    for (auto& handle : data->handles) {
        auto& free_queue = data->free_buffers[handle.handle];
        auto& used_queue = data->read_buffers[handle.handle];

        while (free_queue.size() > 0) {
            buffer_destroy(free_queue.front());
            free_queue.pop();
        }

        while (used_queue.size() > 0) {
            buffer_destroy(used_queue.front());
            used_queue.pop();
        }

        CloseHandle(handle.mutex_buffers);
    }

    CloseHandle(data->hThreadEndEvent);
    CloseHandle(data->hThread);

    delete data;
}

NILOOP_API int read_buffer(poll_thread_data* data, TaskHandle task, void* dest, int size) {
    for (int i = 0; i < data->handles.size(); i++) {
        if (data->handles[i].handle == task) {
            auto& taskInfo = data->handles[i];
            auto& read_buffers = data->read_buffers[task];
            auto& free_buffers = data->free_buffers[task];

            WaitForSingleObject(taskInfo.mutex_buffers, INFINITE);
            if (read_buffers.size() == 0) {
                ReleaseMutex(taskInfo.mutex_buffers);
                return 2;
            }

            auto buf = read_buffers.front();
            read_buffers.pop();
            ReleaseMutex(taskInfo.mutex_buffers);

            if (size < buf.len) {
                return 1;
            }
            if (buf.result >= 0) {
                memcpy(dest, buf.pData, buf.len);
            }

            WaitForSingleObject(taskInfo.mutex_buffers, INFINITE);
            free_buffers.emplace(buf);
            ReleaseMutex(taskInfo.mutex_buffers);

            if (buf.result < 0) {
                return buf.result;
            } else {
                return 0;
            }
        }
    }

    return 3;
}

int fill_buffer_analog(const handle_info &handle, buffer* buf) {
    int32 read = 0;

    auto result = DAQmxReadAnalogF64(
        handle.handle,
        handle.samples_per_chan,
        3.0,
        DAQmx_Val_GroupByChannel,
        (float64*) buf->pData,
        handle.buffer_size,
        &read,
        NULL
    );

    return result;
}

int fill_buffer_digital(const handle_info& handle, buffer* buf) {
    int32 read = 0;
    
    auto result = DAQmxReadDigitalU32(
        handle.handle, 
        handle.samples_per_chan, 
        3.0, 
        DAQmx_Val_GroupByChannel, 
        (uInt32*) buf->pData, 
        handle.buffer_size, 
        &read, 
        NULL
    );

    return result;
}

DWORD WINAPI PollThread(LPVOID pdata) {
    auto data = (poll_thread_data*) pdata;
    while (!data->stop) {
        for (auto& handle : data->handles) {
            WaitForSingleObject(handle.mutex_buffers, INFINITE);
            auto& free_buffers = data->free_buffers[handle.handle];
            auto& read_buffers = data->read_buffers[handle.handle];

            if (free_buffers.size() > 0) {
                auto buffer = free_buffers.front();
                free_buffers.pop();
                ReleaseMutex(handle.mutex_buffers);

                switch (handle.type) {
                case TASK_TYPE::TASK_TYPE_ANALOG_INPUT:
                    buffer.result = fill_buffer_analog(handle, &buffer);
                    break;
                case TASK_TYPE::TASK_TYPE_DIGITAL_INPUT:
                    buffer.result = fill_buffer_digital(handle, &buffer);
                    break;
                default:
                    throw std::exception("NILoop: task type not implemented");
                }

                WaitForSingleObject(handle.mutex_buffers, INFINITE);
                read_buffers.push(buffer);
                ReleaseMutex(handle.mutex_buffers);
            } else {
                ReleaseMutex(handle.mutex_buffers);
                Sleep(1);
            }
        }
    }

    SetEvent(data->hThreadEndEvent);
    return 0;
}