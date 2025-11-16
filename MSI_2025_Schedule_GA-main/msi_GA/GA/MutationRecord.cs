using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace msi_GA.GA
{
    public class MutationRecord
    {
        public Gen Original { get; }
        public Gen Mutated { get; }
        public int WorkerIndex { get; }
        public int ShiftIndex { get; }
        public float Benefit { get; }

        public MutationRecord(Gen original, Gen mutated, int workerIndex,
                             int shiftIndex, float benefit)
        {
            Original = original;
            Mutated = mutated;
            WorkerIndex = workerIndex;
            ShiftIndex = shiftIndex;
            Benefit = benefit;
        }
    }
}
