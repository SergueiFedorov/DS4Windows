using HidLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DS4Library
{
    public class DS4ControllerBT : DS4Controller
    {
        private const int BT_OUTPUT_REPORT_LENGTH = 78;
        private const int BT_INPUT_REPORT_LENGTH = 547;

        private byte[] _btInputReport;

        public DS4ControllerBT(DS4Device owner, HidDevice device)
            : base(owner, device, new byte[BT_INPUT_REPORT_LENGTH - 2], new byte[BT_OUTPUT_REPORT_LENGTH], new byte[BT_OUTPUT_REPORT_LENGTH])
        {
            this._btInputReport = new byte[BT_INPUT_REPORT_LENGTH];
        }

        public override bool WriteOutput()
        {
            lock (this._outputReport)
            {
                return _device.WriteOutputReportViaControl(this._outputReport);
            }
        }

        public override void SendOutputReport()
        {
            //this._ds4Device.setTestRumble();
            //this._ds4Device.setHapticState();

            this._outputReportBuffer[0] = 0x11;
            this._outputReportBuffer[1] = 0x80;
            this._outputReportBuffer[3] = 0xff;
            this._outputReportBuffer[6] = 0x0; //fast motor
            this._outputReportBuffer[7] = 0x0; //slow motor
            this._outputReportBuffer[8] = 0x0; //red
            this._outputReportBuffer[9] = 0x0; //green
            this._outputReportBuffer[10] = 0x0; //blue
            this._outputReportBuffer[11] = 0x0; //flash on duration
            this._outputReportBuffer[12] = 0x0; //flash off duration
        }

        public override void ProcessInput()
        {
            HidDevice.ReadStatus res = this._device.ReadFile(this._btInputReport);
            //readTimeout.Enabled = false;

            if (res == HidDevice.ReadStatus.Success)
            {
                Array.Copy(this._btInputReport, 2, this._inputReport, 0, this._inputReport.Length);
            }
            else
            {
                //this.currentErrors.Add(MacAddress.ToString() + " " + System.DateTime.UtcNow.ToString("o") + "> disconnect due to read failure: " + Marshal.GetLastWin32Error());

                _ds4Device.sendOutputReport(synchronous: true); // Kick Windows into noticing the disconnection.
                _ds4Device.StopOutputUpdate();
                //IsDisconnecting = true;

                //if (Removal != null)
                //{
                //  Removal(this, EventArgs.Empty);
                //}

                //status = InputLoopStatus.DISCONNECTED;
                //continue;

                Status = DS4ControllerStatus.DISCONNECTED;
            }
        }

    }
}
