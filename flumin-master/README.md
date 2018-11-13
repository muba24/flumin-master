# Flumin

A node editor for high-throughput discrete-time dataflow graphs of 
events and constant-rate signals

## Compile

- Make sure solution configuration is either "Debug" or "Release"
- Select Flumin as the start project
- Compile the whole solution

## Things to look out for

NIDAQmx drivers have to be installed, otherwise
the device library won't load.
Also if you only compile Flumin instead of the solution, there won't be any metrics (nodes),
because they are not dependencies of Flumin.

## Project information

- DeviceLibrary: NI metrics and signal generator
- FFTW: C# Wrapper for FFTW, a library for fast computation of FFTs
- Flumin: Main GUI
- MetricFFT: transforms a 1D time-based signal into a 2D STFT time-based signal
- MetricResample: Uses libresample to change the rate of a 1D signal
- MetricTimeDisplay: Plot of 1D and 2D FFT signals as well as events (uses NewOpenGLRenderer)
- NILoop: NI data polling should not be affected by the GC, as it can pause threads for short periods of time. So the main polling loop is written in this C library.
- NewOpenGLRenderer: Library for rendering signals in OpenGL using OpenTK
- NodeSystemLib: Not used anymore
- NodeSystemLib2: Main processing system for graphs
- PropertyGrid: Custom property grid to allow easy validation of inputs and value units
- [QuickFont](https://github.com/opcon/QuickFont): OpenGL (OpenTK) text renderer
- [ToolBox](http://www.codeproject.com/Articles/8658/Another-ToolBox-Control): Written by Aju George
- [WinFormsUI](https://github.com/dockpanelsuite/dockpanelsuite): Docking library