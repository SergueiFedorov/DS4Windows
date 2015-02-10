using HidLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DS4Library
{
    public enum DS4ControllerStatus { CONNECTED, DISCONNECTED }

    public abstract class DS4Controller
    {
        private const double LATENCY_THRESHOLD = 10.0d;

        protected byte[] _inputReport;
        protected byte[] _outputReport;
        protected byte[] _outputReportBuffer;

        protected HidDevice _device;

        private DS4HapticState _hepticState;
        LatencyCounter _latencyCounter;

        //public DS4State currentState = new DS4State();

        private DS4ControllerStatus _Status;
        public DS4ControllerStatus Status
        {
            get
            {
                return _Status;
            }
            protected set
            {
                this._Status = value;
            }
        }

        public Double AverageLatency
        {
            get
            {
                return this._latencyCounter.Latency;
            }
        }

        public string MacAddress
        {
            get
            {
                return this._device.readSerial();
            }
        }

        public bool IsPastLatencyThreshold
        {
            get
            {
                return this.AverageLatency >= LATENCY_THRESHOLD;
            }
        }

        public DS4Touchpad TouchPad { get; private set; }

        protected DS4Device _ds4Device;

        public event EventHandler<EventArgs> Removal = null;

        protected DS4Controller(DS4Device owner, HidDevice device, byte[] inputReport, byte[] outputReport, byte[] outputReportBuffer)
        {
            this._device = device;

            this._inputReport = inputReport;
            this._outputReport = outputReport;
            this._outputReportBuffer = outputReportBuffer;

            this._hepticState = new DS4HapticState();
            this._latencyCounter = new LatencyCounter(200);

            this._ds4Device = owner;

            this.TouchPad = new DS4Touchpad();
        }

        public abstract bool WriteOutput();
        public abstract void ProcessInput();

        public abstract void SendOutputReport();

        public void ProcessOutput()
        {
            int lastError = 0;
            if (this.WriteOutput())
            {
                lastError = 0;
                if (this._hepticState.IsRumbleSet()) // repeat test rumbles periodically; rumble has auto-shut-off in the DS4 firmware
                {
                    Monitor.Wait(this._outputReport, 10000); // DS4 firmware stops it after 5 seconds, so let the motors rest for that long, too.
                }
                else
                {
                    Monitor.Wait(this._outputReport);
                }
            }
            else
            {
                int thisError = Marshal.GetLastWin32Error();
                if (lastError != thisError)
                {
                    Console.WriteLine(MacAddress.ToString() + " " + System.DateTime.UtcNow.ToString("o") + "> encountered write failure: " + thisError);
                    lastError = thisError;
                }
            }
        }

        public DS4State BuildCurrentState(byte[] inputReport)
        {
            DS4State currentState = new DS4State();

            DateTime utcNow = System.DateTime.UtcNow;

            currentState.ReportTimeStamp = utcNow;
            currentState.LX = inputReport[1];
            currentState.LY = inputReport[2];
            currentState.RX = inputReport[3];
            currentState.RY = inputReport[4];
            currentState.L2 = inputReport[8];
            currentState.R2 = inputReport[9];

            currentState.Triangle = ((byte)inputReport[5] & (1 << 7)) != 0;
            currentState.Circle = ((byte)inputReport[5] & (1 << 6)) != 0;
            currentState.Cross = ((byte)inputReport[5] & (1 << 5)) != 0;
            currentState.Square = ((byte)inputReport[5] & (1 << 4)) != 0;
            currentState.DpadUp = ((byte)inputReport[5] & (1 << 3)) != 0;
            currentState.DpadDown = ((byte)inputReport[5] & (1 << 2)) != 0;
            currentState.DpadLeft = ((byte)inputReport[5] & (1 << 1)) != 0;
            currentState.DpadRight = ((byte)inputReport[5] & (1 << 0)) != 0;

            //Convert dpad into individual On/Off bits instead of a clock representation
            byte dpad_state = 0;

            dpad_state = (byte)(
            ((currentState.DpadRight ? 1 : 0) << 0) |
            ((currentState.DpadLeft ? 1 : 0) << 1) |
            ((currentState.DpadDown ? 1 : 0) << 2) |
            ((currentState.DpadUp ? 1 : 0) << 3));

            switch (dpad_state)
            {
                case 0: currentState.DpadUp = true; currentState.DpadDown = false; currentState.DpadLeft = false; currentState.DpadRight = false; break;
                case 1: currentState.DpadUp = true; currentState.DpadDown = false; currentState.DpadLeft = false; currentState.DpadRight = true; break;
                case 2: currentState.DpadUp = false; currentState.DpadDown = false; currentState.DpadLeft = false; currentState.DpadRight = true; break;
                case 3: currentState.DpadUp = false; currentState.DpadDown = true; currentState.DpadLeft = false; currentState.DpadRight = true; break;
                case 4: currentState.DpadUp = false; currentState.DpadDown = true; currentState.DpadLeft = false; currentState.DpadRight = false; break;
                case 5: currentState.DpadUp = false; currentState.DpadDown = true; currentState.DpadLeft = true; currentState.DpadRight = false; break;
                case 6: currentState.DpadUp = false; currentState.DpadDown = false; currentState.DpadLeft = true; currentState.DpadRight = false; break;
                case 7: currentState.DpadUp = true; currentState.DpadDown = false; currentState.DpadLeft = true; currentState.DpadRight = false; break;
                case 8: currentState.DpadUp = false; currentState.DpadDown = false; currentState.DpadLeft = false; currentState.DpadRight = false; break;
            }

            currentState.R3 = ((byte)inputReport[6] & (1 << 7)) != 0;
            currentState.L3 = ((byte)inputReport[6] & (1 << 6)) != 0;
            currentState.Options = ((byte)inputReport[6] & (1 << 5)) != 0;
            currentState.Share = ((byte)inputReport[6] & (1 << 4)) != 0;
            currentState.R1 = ((byte)inputReport[6] & (1 << 1)) != 0;
            currentState.L1 = ((byte)inputReport[6] & (1 << 0)) != 0;

            currentState.PS = ((byte)inputReport[7] & (1 << 0)) != 0;
            currentState.TouchButton = (inputReport[7] & (1 << 2 - 1)) != 0;
            currentState.FrameCounter = (byte)(inputReport[7] >> 2);

            byte[] accel = new byte[6];
            byte[] gyro = new byte[6];

            // Store Gyro and Accel values
            Array.Copy(inputReport, 14, accel, 0, 6);
            Array.Copy(inputReport, 20, gyro, 0, 6);

            try
            {
                bool charging = (inputReport[30] & 0x10) != 0;
                int battery = (inputReport[30] & 0x0f) * 10;

                currentState.Battery = (byte)battery;

                //if (inputReport[30] != priorInputReport30)
                {
                    //priorInputReport30 = inputReport[30];
                    //Console.WriteLine(MacAddress.ToString() + " " + System.DateTime.UtcNow.ToString("o") + "> power subsystem octet: 0x" + inputReport[30].ToString("x02"));
                }
            }
            catch
            {
                //currentErrors.Add("Index out of bounds: battery");
            }

            // XXX DS4State mapping needs fixup, turn touches into an array[4] of structs.  And include the touchpad details there instead.
            try
            {

                for (int touches = inputReport[-1 + DS4Touchpad.TOUCHPAD_DATA_OFFSET - 1], touchOffset = 0; touches > 0; touches--, touchOffset += 9)
                {
                    currentState.TouchPacketCounter = inputReport[-1 + DS4Touchpad.TOUCHPAD_DATA_OFFSET + touchOffset];
                    currentState.Touch1 = (inputReport[0 + DS4Touchpad.TOUCHPAD_DATA_OFFSET + touchOffset] >> 7) != 0 ? false : true; // >= 1 touch detected
                    currentState.Touch1Identifier = (byte)(inputReport[0 + DS4Touchpad.TOUCHPAD_DATA_OFFSET + touchOffset] & 0x7f);
                    currentState.Touch2 = (inputReport[4 + DS4Touchpad.TOUCHPAD_DATA_OFFSET + touchOffset] >> 7) != 0 ? false : true; // 2 touches detected
                    currentState.Touch2Identifier = (byte)(inputReport[4 + DS4Touchpad.TOUCHPAD_DATA_OFFSET + touchOffset] & 0x7f);
                    currentState.TouchLeft = (inputReport[1 + DS4Touchpad.TOUCHPAD_DATA_OFFSET + touchOffset] + ((inputReport[2 + DS4Touchpad.TOUCHPAD_DATA_OFFSET + touchOffset] & 0xF) * 255) >= 1920 * 2 / 5) ? false : true;
                    currentState.TouchRight = (inputReport[1 + DS4Touchpad.TOUCHPAD_DATA_OFFSET + touchOffset] + ((inputReport[2 + DS4Touchpad.TOUCHPAD_DATA_OFFSET + touchOffset] & 0xF) * 255) < 1920 * 2 / 5) ? false : true;
                    // Even when idling there is still a touch packet indicating no touch 1 or 2
                    this.TouchPad.handleTouchpad(inputReport, currentState, touchOffset);
                }
            }
            catch
            {
                //currentErrors.Add("Index out of bounds: touchpad");
            }

            return currentState;
        }
    }
}
