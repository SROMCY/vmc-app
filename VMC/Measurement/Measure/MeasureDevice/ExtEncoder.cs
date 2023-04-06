using System;
using System.Net.Sockets;
using System.Text;
using ACS.SPiiPlusNET;
using Triamec.Tam.Rlid19;
using Axis = ACS.SPiiPlusNET.Axis;

namespace VMC.Measurement
{
    public class ExtEncoder : IMeasureDevice
    {// use this class to implement external encoders e.g. from the grid (connected to ACS controller)
        public int AxisNum { get; set; }
        public Api Client { get; set; }
        public Axis ACSAxis { get; set; }
        public ExtEncoder(Api client, int axis_num = 0)
        {
            AxisNum = axis_num;
            Client = client;
            if(AxisNum == 0){ // X
                ACSAxis = Axis.ACSC_AXIS_0;
            }
            else{ 
                if (AxisNum == 1){ // Y
                    ACSAxis = Axis.ACSC_AXIS_1;
                }
                else{ // Z
                    ACSAxis = Axis.ACSC_AXIS_4;
                } 
            }
        }

        public void SetOffset(double desiredPosition, double measurePosition)
        {
            double val = desiredPosition;
            Client.SetFPosition(ACSAxis, val);
        }

        public double GetFeedback(double param = 0)
        {   double[] outs = new double[10];
            double sum = 0;
            for (int i = 0; i< outs.Length; i++)
            {
                outs[i] = Client.GetFPosition(ACSAxis);
                sum += outs[i];
            }
            double ret = sum / outs.Length;
            return ret;

        }
    }
}
