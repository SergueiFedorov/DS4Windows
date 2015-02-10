using HidLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DS4Library.Controllers
{
    public class DS4ControllerUSB : DS4Controller
    {
        private const int USB_INPUT_REPORT_LENGTH = 64;

        public DS4ControllerUSB(DS4Device owner, HidDevice device)
            : base(owner, device, new byte[USB_INPUT_REPORT_LENGTH], 
                                  new byte[device.Capabilities.OutputReportByteLength], 
                                  new byte[device.Capabilities.OutputReportByteLength])
        {

        }

        public override bool WriteOutput()
        {
            return this._device.WriteOutputReportViaInterrupt(this._outputReport, 8);
        }

        public override void ProcessInput()
        {
            HidDevice.ReadStatus res = this._device.ReadFile(this._inputReport);
            //readTimeout.Enabled = false;
            if (res != HidDevice.ReadStatus.Success)
            {
                /*
                //Console.WriteLine(MacAddress.ToString() + " " + System.DateTime.UtcNow.ToString("o") + "> disconnect due to read failure: " + Marshal.GetLastWin32Error());
                this.currentErrors.Add(MacAddress.ToString() + " " + System.DateTime.UtcNow.ToString("o") + "> disconnect due to read failure: " + Marshal.GetLastWin32Error());

                this._ds4Device.StopOutputUpdate();
                IsDisconnecting = true;
                if (Removal != null)
                {
                    Removal(this, EventArgs.Empty);
                }

                status = InputLoopStatus.DISCONNECTED;
                continue;*/

                this.Status = DS4ControllerStatus.DISCONNECTED;
            }
        }

        public override void SendOutputReport()
        {
            this._outputReportBuffer[0] = 0x05;
            this._outputReportBuffer[1] = 0xff;
            this._outputReportBuffer[4] = 0x0; //fast motor
            this._outputReportBuffer[5] = 0x0; //slow  motor
            this._outputReportBuffer[6] = 0x0; //red
            this._outputReportBuffer[7] = 0x0; //green
            this._outputReportBuffer[8] = 0x0; //blue
            this._outputReportBuffer[9] = 0x0; //flash on duration
            this._outputReportBuffer[10] = 0x0; //flash off duration
        }
    }
}
