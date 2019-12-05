/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

using Microsoft.DirectX.DirectInput;

namespace QuuxPlayer
{
    internal sealed class Gamepad : IDisposable
    {
        private enum Mode { None, Normal, Thrustmaster }

        public bool Enabled { get; private set; }

        private bool joy0FirstReport = true;
        private bool joy1FirstReport = true;
        private long suppressJoy0Until = long.MaxValue;
        private long suppressJoy1Until = long.MaxValue;
        private Mode mode = Mode.None;

        private const long SUPPRESS_JOY_TICKS = 20;

        private QActionType[] buttonActions = new QActionType[NUM_BUTTONS];
        private QActionType[] altButtonActions = new QActionType[NUM_BUTTONS];

        private bool[] altButtonRepeats = new bool[NUM_BUTTONS];

        private const int GAMEPAD_POLL_TICKS = 12;

        private const int GAMEPAD_JOY_REPEAT_TICKS = 8;

        private const int GAMEPAD_JOYSTICK_RANGE = 1000;
        
        private const float GAMEPAD_JOY_SPEED_FACTOR = 0.09f;
        
        private const int JOYSTICK_DEAD_ZONE = GAMEPAD_JOYSTICK_RANGE *   2 / 10;
        private const int JOYSTICK_SLOW_ZONE = GAMEPAD_JOYSTICK_RANGE *   4 / 10;
        private const int JOYSTICK_STEADY_ZONE = GAMEPAD_JOYSTICK_RANGE * 6 / 10;
        private const int JOYSTICK_ACCEL_ZONE = GAMEPAD_JOYSTICK_RANGE *  8 / 10;

        private const int NUM_BASIC_BUTTONS = 12;
        private const int NUM_BUTTONS = NUM_BASIC_BUTTONS + 4; // pov
        
        private const int POV_REPEAT_DELAY = 500;
        private const int POV_REPEAT_RATE = 150;
        private const float JOYSTICK_ACCEL_FACTOR = 1.05f;

        private Device gamePad;
        private JoystickState state;
        private Form owner;
        private Timer pollTimer;

        private Controller controller;

        private IActionHandler actionHandler;

        private enum ButtonState { Suspended,
                                   Ready,
                                   AlternateDelay1,
                                   AlternateDelay2,
                                   AlternateDelay3,
                                   AlternateDelay4,
                                   AlternateDelay5,
                                   AlternateDelay6,
                                   AlternateDelay7,
                                   AlternateDelay8,
                                   AlternateDelay9,
                                   AlternateDelay10,
                                   AlternateDelay11,
                                   AlternateDelay12,
                                   AlternateDelay13,
                                   AlternateDelay14,
                                   AlternateDelay15,
                                   AlternateDelay16,
                                   AlternateReady }

        private ButtonState[] buttonCache = new ButtonState[NUM_BUTTONS];
        private long tick = 0;
        private float joyAccel0 = 0;
        private float joyAccel1 = 0;

        private bool analogEnabled = true;

        public Gamepad(Form Owner)
        {
            this.Enabled = false;

            owner = Owner;

            controller = Controller.GetInstance();
            actionHandler = controller;

            GamePadMode = Mode.Normal;
        }

        private void mapInputs()
        {
            for (int i = 0; i < NUM_BUTTONS; i++)
            {
                buttonActions[i] = QActionType.None;
                altButtonActions[i] = QActionType.None;
                altButtonRepeats[i] = false;
            }

            // normal buttons

            switch (GamePadMode)
            {
                case Mode.Normal:

                    buttonActions[0] = QActionType.Previous;
                    altButtonActions[0] = QActionType.ScanBack;

                    buttonActions[1] = QActionType.VolumeDown;
                    altButtonActions[1] = QActionType.RepeatAction;
                    break;
                case Mode.Thrustmaster:
                    buttonActions[1] = QActionType.Previous;
                    altButtonActions[1] = QActionType.ScanBack;

                    buttonActions[0] = QActionType.VolumeDown;
                    altButtonActions[0] = QActionType.RepeatAction;
                    break;
                default:
                    throw new Exception("GamePad Init Error");
            }
            buttonActions[2] = QActionType.Next;
            altButtonActions[2] = QActionType.ScanFwd;

            buttonActions[3] = QActionType.VolumeUp;
            altButtonActions[3] = QActionType.RepeatAction;

            buttonActions[4] = QActionType.AdvanceScreenWithoutMouse;
            altButtonActions[4] = QActionType.ShowTrackAndAlbumDetails;

            buttonActions[5] = QActionType.PlaySelectedTracks;
            altButtonActions[5] = QActionType.PlayThisAlbum;

            buttonActions[6] = QActionType.SelectNextEqualizer;
            altButtonActions[6] = QActionType.ToggleEqualizer;

            buttonActions[7] = QActionType.Pause;
            altButtonActions[7] = QActionType.Stop;

            buttonActions[8] = QActionType.AddToNowPlayingAndAdvance;
            altButtonActions[8] = QActionType.ToggleRadioMode;

            buttonActions[9] = QActionType.HTPCMode;
            altButtonActions[9] = QActionType.ToggleGamepadHelp;

            buttonActions[10] = QActionType.AdvanceSortColumn;
            buttonActions[11] = QActionType.Shuffle;

            // pov buttons

            buttonActions[12] = QActionType.FilterSelectedArtist;
            altButtonActions[12] = QActionType.FilterSelectedAlbum;

            buttonActions[13] = QActionType.NextFilter;
            altButtonActions[13] = QActionType.FindPlayingTrack;

            buttonActions[14] = QActionType.ReleaseCurrentFilter;
            altButtonActions[14] = QActionType.ReleaseAllFilters;

            buttonActions[15] = QActionType.PreviousFilter;
            altButtonActions[15] = QActionType.ViewNowPlaying;

            altButtonRepeats[0] = true;
            altButtonRepeats[2] = true;
        }

        public IActionHandler TempHandler
        {
            get { return actionHandler; }
            set
            {
                if (value == null)
                    actionHandler = controller;
                else
                    actionHandler = value;
            }
        }

        public bool Locked { get; set; }

        public bool AnalogEnabled
        {
            get { return analogEnabled; }
            set { analogEnabled = value; }
        }

        public bool Start()
        {
            try
            {
                if (!init())
                {
                    Enabled = false;
                    return false;
                }

                Enabled = true;

                pollTimer = new Timer();
                pollTimer.Interval = GAMEPAD_POLL_TICKS;
                pollTimer.Tick += new EventHandler(pollTick);
                pollTimer.Start();
            }
            catch
            {
                return false;
            }
            return true;
        }
        public void Dispose()
        {
            this.Enabled = false;

            if (pollTimer != null)
            {
                pollTimer.Stop();
                pollTimer.Dispose();
                System.Threading.Thread.Sleep(10);
            }
            if (gamePad != null)
            {
                gamePad.Dispose();
                gamePad = null;
            }
        }
        public int GetJoystickAccelleration(int JoystickNum)
        {
            return (JoystickNum == 0) ? (int)joyAccel0 : (int)joyAccel1;
        }

        private Mode GamePadMode
        {
            get { return mode; }
            set
            {
                if (mode != value)
                {
                    mode = value;
                    mapInputs();
                }
            }
        }

        private void pollTick(object sender, EventArgs e)
        {
            updateGamepadData();
            tick++;

            byte[] b = state.GetButtons();

            updatePOVButtons(b);

            for (int i = 0; i < b.Length && i < NUM_BUTTONS; i++)
            {
                if ((b[i] & 0x80) != 0)
                {
                    switch (buttonCache[i])
                    {
                        case ButtonState.Ready:
                            switch (altButtonActions[i])
                            {
                                case QActionType.None:
                                    doAction(buttonActions[i]);
                                    buttonCache[i] = ButtonState.Suspended;
                                    break;
                                case QActionType.RepeatAction:
                                    doAction(buttonActions[i]);
                                    buttonCache[i] = ButtonState.Ready;
                                    break;
                                default:
                                    buttonCache[i] = ButtonState.AlternateDelay1;
                                    break;
                            }
                            break;
                        case ButtonState.AlternateDelay1:
                        case ButtonState.AlternateDelay2:
                        case ButtonState.AlternateDelay3:
                        case ButtonState.AlternateDelay4:
                        case ButtonState.AlternateDelay5:
                        case ButtonState.AlternateDelay6:
                        case ButtonState.AlternateDelay7:
                        case ButtonState.AlternateDelay8:
                        case ButtonState.AlternateDelay9:
                        case ButtonState.AlternateDelay10:
                        case ButtonState.AlternateDelay11:
                        case ButtonState.AlternateDelay12:
                        case ButtonState.AlternateDelay13:
                        case ButtonState.AlternateDelay14:
                        case ButtonState.AlternateDelay15:
                        case ButtonState.AlternateDelay16:
                            buttonCache[i]++;
                            break;
                        case ButtonState.AlternateReady:
                            if (!altButtonRepeats[i])
                                buttonCache[i] = ButtonState.Suspended;
                            doAction(altButtonActions[i]);
                            break;
                    }
                }
                else
                {
                    switch (buttonCache[i])
                    {
                        case ButtonState.Suspended:
                            buttonCache[i] = ButtonState.Ready;
                            break;
                        case ButtonState.AlternateDelay1:
                        case ButtonState.AlternateDelay2:
                        case ButtonState.AlternateDelay3:
                        case ButtonState.AlternateDelay4:
                        case ButtonState.AlternateDelay5:
                        case ButtonState.AlternateDelay6:
                        case ButtonState.AlternateDelay7:
                        case ButtonState.AlternateDelay8:
                        case ButtonState.AlternateDelay9:
                        case ButtonState.AlternateDelay10:
                        case ButtonState.AlternateDelay11:
                        case ButtonState.AlternateDelay12:
                        case ButtonState.AlternateDelay13:
                        case ButtonState.AlternateDelay14:
                        case ButtonState.AlternateDelay15:
                        case ButtonState.AlternateDelay16:
                            doAction(buttonActions[i]);
                            buttonCache[i] = ButtonState.Ready;
                            break;
                        case ButtonState.AlternateReady:
                            buttonCache[i] = ButtonState.Ready;
                            break;
                    }
                }
            }

            if (analogEnabled)
            {
                if (state.Y < -JOYSTICK_DEAD_ZONE)
                {
                    if (suppressJoy0Until == long.MaxValue || tick >= suppressJoy0Until)
                    {
                        if ((state.Y < -JOYSTICK_SLOW_ZONE) || (tick % GAMEPAD_JOY_REPEAT_TICKS == 0) || (joyAccel0 == 0.0f))
                        {
                            if (state.Y < -JOYSTICK_ACCEL_ZONE)
                                joyAccel0 = Math.Max((float)state.Y * GAMEPAD_JOY_SPEED_FACTOR, Math.Min(-1F, joyAccel0 * JOYSTICK_ACCEL_FACTOR));
                            else if (state.Y > -JOYSTICK_STEADY_ZONE)
                                joyAccel0 = Math.Max((float)state.Y * GAMEPAD_JOY_SPEED_FACTOR, Math.Min(-1F, joyAccel0 / JOYSTICK_ACCEL_FACTOR));

                            doAction(QActionType.SelectPreviousItemGamePadLeft);
                        }
                    }
                    if (joy0FirstReport)
                    {
                        joy0FirstReport = false;
                        suppressJoy0Until = tick + SUPPRESS_JOY_TICKS;
                    }
                }
                else if (state.Y > JOYSTICK_DEAD_ZONE)
                {
                    if (suppressJoy0Until == long.MaxValue || tick >= suppressJoy0Until)
                    {
                        if ((state.Y > JOYSTICK_SLOW_ZONE) || (tick % GAMEPAD_JOY_REPEAT_TICKS == 0) || (joyAccel0 == 0.0f))
                        {
                            if (state.Y > JOYSTICK_ACCEL_ZONE)
                                joyAccel0 = Math.Min((float)state.Y * GAMEPAD_JOY_SPEED_FACTOR, Math.Max(1F, joyAccel0 * JOYSTICK_ACCEL_FACTOR));
                            else if (state.Y < JOYSTICK_STEADY_ZONE)
                                joyAccel0 = Math.Min((float)state.Y * GAMEPAD_JOY_SPEED_FACTOR, Math.Max(1F, joyAccel0 / JOYSTICK_ACCEL_FACTOR));

                            doAction(QActionType.SelectNextItemGamePadLeft);
                        }
                    }
                    if (joy0FirstReport)
                    {
                        joy0FirstReport = false;
                        suppressJoy0Until = tick + SUPPRESS_JOY_TICKS;
                    }
                }
                else
                {
                    joyAccel0 = 0.0f;
                    joy0FirstReport = true;
                    suppressJoy0Until = long.MaxValue;
                }
                int rightAnalog = ((GamePadMode == Mode.Normal) ? state.Rz : state.GetSlider()[0]);

                if (rightAnalog < -JOYSTICK_DEAD_ZONE)
                {
                    if (suppressJoy1Until == long.MaxValue || tick >= suppressJoy1Until)
                    {
                        if ((rightAnalog < -JOYSTICK_SLOW_ZONE) || (tick % GAMEPAD_JOY_REPEAT_TICKS == 0) || (joyAccel1 == 0.0f))
                        {
                            if (rightAnalog < -JOYSTICK_ACCEL_ZONE)
                                joyAccel1 = Math.Max((float)rightAnalog * GAMEPAD_JOY_SPEED_FACTOR, Math.Min(-1F, joyAccel1 * JOYSTICK_ACCEL_FACTOR));
                            else if (rightAnalog > -JOYSTICK_STEADY_ZONE)
                                joyAccel1 = Math.Max((float)rightAnalog * GAMEPAD_JOY_SPEED_FACTOR, Math.Min(-1F, joyAccel1 / JOYSTICK_ACCEL_FACTOR));

                            doAction(QActionType.SelectPreviousItemGamePadRight);
                        }
                    }
                    if (joy1FirstReport)
                    {
                        joy1FirstReport = false;
                        suppressJoy1Until = tick + SUPPRESS_JOY_TICKS;
                    }
                }
                else if (rightAnalog > JOYSTICK_DEAD_ZONE)
                {
                    if (suppressJoy1Until == long.MaxValue || tick >= suppressJoy1Until)
                    {
                        if ((rightAnalog > JOYSTICK_SLOW_ZONE) || (tick % GAMEPAD_JOY_REPEAT_TICKS == 0) || (joyAccel1 == 0.0f))
                        {
                            if (rightAnalog > JOYSTICK_ACCEL_ZONE)
                                joyAccel1 = Math.Min((float)rightAnalog * GAMEPAD_JOY_SPEED_FACTOR, Math.Max(1F, joyAccel1 * JOYSTICK_ACCEL_FACTOR));
                            else if (rightAnalog < JOYSTICK_STEADY_ZONE)
                                joyAccel1 = Math.Min((float)rightAnalog * GAMEPAD_JOY_SPEED_FACTOR, Math.Max(1F, joyAccel1 / JOYSTICK_ACCEL_FACTOR));

                            doAction(QActionType.SelectNextItemGamePadRight);
                        }
                    }
                    if (joy1FirstReport)
                    {
                        joy1FirstReport = false;
                        suppressJoy1Until = tick + SUPPRESS_JOY_TICKS;
                    }
                }
                else
                {
                    joyAccel1 = 0.0f;
                    joy1FirstReport = true;
                    suppressJoy1Until = long.MaxValue;
                }
            }
            else // not analog enabled
            {
                joyAccel0 = 0.0f;
                joyAccel1 = 0.0f;
            }
        }
        private void doAction(QActionType Action)
        {
            if (!Locked)
            {
                actionHandler.RequestAction(Action);
                switch (Action)
                {
                    case QActionType.PlaySelectedTracks:
                    case QActionType.Pause:
                        frmGlobalInfoBox.Show(controller, frmGlobalInfoBox.ActionType.PlayPause);
                        break;
                    case QActionType.Stop:
                        frmGlobalInfoBox.Show(controller, frmGlobalInfoBox.ActionType.Stop);
                        break;
                    case QActionType.Next:
                        frmGlobalInfoBox.Show(controller, frmGlobalInfoBox.ActionType.Next);
                        break;
                    case QActionType.Previous:
                        frmGlobalInfoBox.Show(controller, frmGlobalInfoBox.ActionType.Previous);
                        break;
                    case QActionType.VolumeUp:
                        frmGlobalInfoBox.Show(controller, frmGlobalInfoBox.ActionType.VolumeUp);
                        break;
                    case QActionType.VolumeDown:
                        frmGlobalInfoBox.Show(controller, frmGlobalInfoBox.ActionType.VolumeDown);
                        break;
                }
            }
        }
        private int updatePOVButtons(byte[] b)
        {
            int pov = state.GetPointOfView()[0];

            switch (pov)
            {
                case 0:
                    b[NUM_BASIC_BUTTONS] = 0x80;
                    b[NUM_BASIC_BUTTONS + 1] = 0;
                    b[NUM_BASIC_BUTTONS + 2] = 0;
                    b[NUM_BASIC_BUTTONS + 3] = 0;
                    break;
                case 4500:
                case 9000:
                case 13500:
                    b[NUM_BASIC_BUTTONS] = 0;
                    b[NUM_BASIC_BUTTONS + 1] = 0x80;
                    b[NUM_BASIC_BUTTONS + 2] = 0;
                    b[NUM_BASIC_BUTTONS + 3] = 0;
                    break;
                case 18000:
                    b[NUM_BASIC_BUTTONS] = 0;
                    b[NUM_BASIC_BUTTONS + 1] = 0;
                    b[NUM_BASIC_BUTTONS + 2] = 0x80;
                    b[NUM_BASIC_BUTTONS + 3] = 0;
                    break;
                case 22500:
                case 27000:
                case 31500:
                    b[NUM_BASIC_BUTTONS] = 0;
                    b[NUM_BASIC_BUTTONS + 1] = 0;
                    b[NUM_BASIC_BUTTONS + 2] = 0;
                    b[NUM_BASIC_BUTTONS + 3] = 0x80;
                    break;
                default:
                    b[NUM_BASIC_BUTTONS] = 0;
                    b[NUM_BASIC_BUTTONS + 1] = 0;
                    b[NUM_BASIC_BUTTONS + 2] = 0;
                    b[NUM_BASIC_BUTTONS + 3] = 0;
                    break;

            }
            return pov;
        }
        private bool init()
        {
            gamePad = null;

            foreach (DeviceInstance device in Manager.GetDevices(DeviceClass.GameControl, EnumDevicesFlags.AttachedOnly))
            {
                gamePad = new Device(device.InstanceGuid);
                break;
            }

            if (gamePad == null)
            {
                return false;
            }

            if (gamePad.DeviceInformation.ProductName.ToLowerInvariant().Contains("firestorm"))
                GamePadMode = Mode.Thrustmaster;

            gamePad.SetDataFormat(DeviceDataFormat.Joystick);
            gamePad.SetCooperativeLevel(owner, CooperativeLevelFlags.Exclusive | CooperativeLevelFlags.Background);
            foreach (DeviceObjectInstance d in gamePad.Objects)
            {
                if ((d.ObjectId & (int)DeviceObjectTypeFlags.Axis) != 0)
                {
                    gamePad.Properties.SetRange(ParameterHow.ById, d.ObjectId, new InputRange(-GAMEPAD_JOYSTICK_RANGE, GAMEPAD_JOYSTICK_RANGE));
                }
            }
            return true;
        }
        private void updateGamepadData()
        {
            if (gamePad == null)
                return;

            try
            {
                gamePad.Poll();
            }
            catch (InputException inputex)
            {
                if ((inputex is NotAcquiredException) || (inputex is InputLostException))
                {
                    try
                    {
                        gamePad.Acquire();
                    }
                    catch (InputException)
                    {
                        this.Enabled = false;
                        return;
                    }
                }
            }

            try
            {
                state = gamePad.CurrentJoystickState;
            }
            catch (InputException)
            {
                return;
            }
        }
    }
}
