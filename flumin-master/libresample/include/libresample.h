/**********************************************************************

  resample.h

  Real-time library interface by Dominic Mazzoni

  Based on resample-1.7:
    http://www-ccrma.stanford.edu/~jos/resample/

  Dual-licensed as LGPL and BSD; see README.md and LICENSE* files.

**********************************************************************/

#ifndef LIBRESAMPLE_INCLUDED
#define LIBRESAMPLE_INCLUDED

// 11.7.2016: modified to accomodate for varying sample types - Arne Elster
#include "resample_defs.h"

#ifdef __cplusplus
extern "C" {
#endif	/* __cplusplus */

void *resample_open(int      highQuality,
                    double   minFactor,
                    double   maxFactor);

void *resample_dup(const void *handle);

int resample_get_filter_width(const void *handle);

int resample_process(void   *handle,
                     double  factor,
                     sample_type  *inBuffer,
                     int     inBufferLen,
                     int     lastFlag,
                     int    *inBufferUsed,
                     sample_type  *outBuffer,
                     int     outBufferLen);

void resample_close(void *handle);

#ifdef __cplusplus
}		/* extern "C" */
#endif	/* __cplusplus */

#endif /* LIBRESAMPLE_INCLUDED */
