/**********************************************************************

  resamplesubs.c

  Real-time library interface by Dominic Mazzoni

  Based on resample-1.7:
    http://www-ccrma.stanford.edu/~jos/resample/

  Dual-licensed as LGPL and BSD; see README.md and LICENSE* files.

**********************************************************************/

/* Definitions */
#include "resample_defs.h"

/*
 * FilterUp() - Applies a filter to a given sample when up-converting.
 * FilterUD() - Applies a filter to a given sample when up- or down-
 */

sample_type lrsFilterUp(sample_type Imp[], sample_type ImpD[], UWORD Nwing, BOOL Interp,
                        sample_type *Xp, double Ph, int Inc);

sample_type lrsFilterUD(sample_type Imp[], sample_type ImpD[], UWORD Nwing, BOOL Interp,
                        sample_type *Xp, double Ph, int Inc, double dhb);

void lrsLpFilter(double c[], int N, double frq, double Beta, int Num);
