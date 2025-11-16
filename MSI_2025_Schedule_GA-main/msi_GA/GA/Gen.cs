using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace msi_GA.GA
{

    public enum OccupationType
    {
        Operator,
        ControlTechnician,
        MaintenanceEngineer,
        RadiationTechnician,
        ElectricalEngineer,
        ChemistryTechnician
    }


    public class Gen : Operations
    {


        public int[][] _schedule;
        private double _fitness;
        private double _HoursScore;
        private double _ShiftBreakScore;
        private double _workerDispersionScore;
        public double _maxShiftsScore;
        public double _preferenceScore;
        private double _TypeOfWorkerPerShift;
        private bool _validHardConstraints = false;
        public int days = 7;
        public int workers = 0;
        Random random = new Random();

        public Gen(bool IsconstantAware, int workers)
        {

            _schedule = new int[workers][];

            for (int i = 0; i < workers; i++)
            {
                _schedule[i] = new int[3 * days];
            }

            PickGeneration(IsconstantAware);
            _fitness = GetNormalizedFitness(_schedule);

            _HoursScore = HoursMeet(_schedule);
            _ShiftBreakScore = ShiftBreak(_schedule);
            _workerDispersionScore = WorkerDispersionGeneral(_schedule);
            _maxShiftsScore = MaxShifts(_schedule);
            _preferenceScore = Preferences(_schedule);
            _TypeOfWorkerPerShift = EachWorkerTypePerShifts(_schedule);

            if (HoursScore >= 0.8 && ShiftBreakScore == 1 && MaxShiftScore == 1)
            {
                ValidHardConstraints = true;
            }
            else ValidHardConstraints = false;

        }

        public Gen()
        {


        }


        public double PreferencesScore
        {
            get { return _preferenceScore; }
            set { _preferenceScore = value; }
        }
        public double ShiftBreakScore
        {
            get { return _ShiftBreakScore; }
            set { _ShiftBreakScore = value; }

        }
        public double WorkerDispersionScore
        {
            get { return _workerDispersionScore; }
            set { _workerDispersionScore = value; }
        }

        public double HoursScore
        {
            get { return _HoursScore; }
            set { _HoursScore = value; }

        }

        public double MaxShiftScore
        {
            get { return _maxShiftsScore; }
            set { _maxShiftsScore = value; }
        }

        public double TypeOfWorkerPerShift
        {
            get { return _TypeOfWorkerPerShift; }
            set { _TypeOfWorkerPerShift = value; }
        }

        public int Days
        {
            get { return days; }
            set { days = value; }
        }


        public double Fitness
        {
            get { return _fitness; }
            set { _fitness = value; }
        }

        public int[][] Schedule
        {
            get { return _schedule; }
            set { _schedule = value; }
        }

        public int WorketsAmount
        {

            get { return _schedule.Length; }
        }

        public int ShiftsAmount
        {
            get { return days * 3; }
        }

        public bool ValidHardConstraints
        {
            get { return _validHardConstraints; }
            set { _validHardConstraints = value; }
        }
        public void PickGeneration(bool generation)
        {
            if (generation)
            {
                ConstantAwareGeneration();

            }
            else
            {
                GenerateSchedule();
            }
        }

        public void GenerateSchedule()
        {


            for (int i = 0; i < _schedule.Length; i++)
            {

                for (int j = 0; j < _schedule[i].Length; j = j + 3)
                {


                    for (int k = j; k < j + 3; k++)
                    {
                        _schedule[i][k] = random.Next(0, 2);

                    }


                }
            }



        }



        public void ConstantAwareGeneration()
        {

            for (int i = 0; i < _schedule.Length; i++)
            {

                for (int j = 0; j < _schedule[i].Length; j = j + 3)
                {


                    for (int k = j; k < j + 3; k++)
                    {


                        _schedule[i][k] = random.Next(0, 2);

                        if ((k != 0 && _schedule[i][k - 1] == 1) || (k > 1 && _schedule[i][k - 2] == 1))
                        {
                            _schedule[i][k] = 0;
                        }



                    }


                }
            }


        }


        public Gen Clone()
        {
            Gen clone = new Gen(this.workers); // Create new instance

            // Deep copy the schedule array
            clone._schedule = new int[this._schedule.Length][];
            for (int i = 0; i < this._schedule.Length; i++)
            {
                clone._schedule[i] = new int[this._schedule[i].Length];
                Array.Copy(this._schedule[i], clone._schedule[i], this._schedule[i].Length);
            }

            // Copy all other properties
            clone.Fitness = this.Fitness;
            clone.HoursScore = this.HoursScore;
            clone.ShiftBreakScore = this.ShiftBreakScore;
            clone.WorkerDispersionScore = this.WorkerDispersionScore;
            clone.MaxShiftScore = this.MaxShiftScore;
            clone.TypeOfWorkerPerShift = this.TypeOfWorkerPerShift;

            return clone;
        }

        // Constructor for cloning
        public Gen(int workers)
        {
            this.workers = workers;
            // Initialize but don't fill schedule - we'll copy it
            this._schedule = new int[workers][];
            for (int i = 0; i < workers; i++)
            {
                this._schedule[i] = new int[ShiftsAmount];



            }




        }






    }



}
