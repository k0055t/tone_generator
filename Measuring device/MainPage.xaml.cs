#if ANDROID
using System.Diagnostics;
using Android.Bluetooth;
using Java.Util;
using Android.Media;
using Android.Util;
using Android.Content;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Java.IO;
using System.Data;
using System.Runtime.CompilerServices;
using Microsoft.Maui.ApplicationModel;
using Android.Service.Autofill;
using Xamarin.Google.Crypto.Tink.Subtle;

namespace Measuring_device;

public partial class MainPage : ContentPage

{

    
	public MainPage()
	{
		InitializeComponent();
        MicIn.startRecorder();


    }
    async void updateValues()
    {
        MicIn.startRecorder();
        var soundBar = (ProgressBar)this.FindByName("Bar");
        soundBar.Progress = 0;
        while (true)
        {
            soundBar.Progress = MicIn.convertdDb(MicIn.getAmplitudeEMA());
        }
    }


    bool connected = false;
    BluetoothConnector connector;
    private async void OnCounterClicked(object sender, EventArgs e)
    {

        await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        await Permissions.RequestAsync<Permissions.Speech>();

        //const string ArduinoBluetoothTransceiverName = "DESKTOP-MIHFIH7";

        connector = new BluetoothConnector();

        // await RequestBluetoothAccess();

        //Gets a list of all connected Bluetooth devices
        var ConnectedDevices = connector.GetConnectedDevices();
        System.Diagnostics.Debug.WriteLine("Zarizeni");
        foreach (var ConnectedDevice in ConnectedDevices)
        {
            System.Diagnostics.Debug.WriteLine(ConnectedDevice);
        }
        connector.Connect("DESKTOP-MIHFIH7");


        ////Debug.Write(ConnectedDevices);
        //Connects to the Arduino
        //var arduino = ConnectedDevices.FirstOrDefault(d => d == ArduinoBluetoothTransceiverName);
        //connector.Connect(arduino);
        connected = true;
        await DisplayAlert("Connected", "Connection succesfull", "OK");

    }

    private async void OnSendClicked(object sender, EventArgs e)
    {
        if (connected)
        {
            connector.SendData();
            Debug.WriteLine(MicIn.convertdDb(MicIn.getAmplitudeEMA()));
        }
    }

    public class BluetoothConnector : IBluetoothConnector
    {
        /// <inheritdoc />
        public List<string> GetConnectedDevices()
        {
            _adapter = BluetoothAdapter.DefaultAdapter;
            if (_adapter == null)
            {
                throw new Exception("No Bluetooth adapter found.");
            }
            else if (_adapter.IsEnabled)
            {
                if (_adapter.BondedDevices.Count > 0)
                {
                    return _adapter.BondedDevices.Select(d => d.Name).ToList();
                }
            }
            else
            {
                System.Console.Write("Bluetooth is not enabled on device");
            }
            return new List<string>();
        }

        /// <inheritdoc />
        public void Connect(string deviceName)
        {
            var device = _adapter.BondedDevices.FirstOrDefault(d => d.Name == deviceName);
            _socket = device.CreateRfcommSocketToServiceRecord(UUID.FromString(SspUdid));

            _socket.Connect();

            // Write data to the device to trigger LED

        }

        public async void SendData()
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                MainActivity activity = (MainActivity)Platform.CurrentActivity;

                activity.Window.AddFlags(Android.Views.WindowManagerFlags.KeepScreenOn);

            });

            byte[] dataOut1 = new byte[] {0b01};
            await _socket.OutputStream.WriteAsync(dataOut1, 0, dataOut1.Length);
            while (true)
            {
                byte datain = 0xFF;

                while (datain != 0b01)
                {
                    datain = (byte)_socket.InputStream.ReadByte();
                }

                double amplitudeAverage = 0;
                int i = 0;
                while (datain != 0b10)
                {
                    datain = (byte)_socket.InputStream.ReadByte();
                    amplitudeAverage += MicIn.convertdDb(MicIn.getAmplitudeEMA());
                    Thread.Sleep(100);
                    i++;
                }
                byte[] dataOut = new byte[8];
                amplitudeAverage /= i;
                dataOut = BitConverter.GetBytes(amplitudeAverage);
                await _socket.OutputStream.WriteAsync(dataOut, 0, dataOut.Length);

            }
        }
        /// <summary>
        /// The standard UDID for SSP
        /// </summary>
        private const string SspUdid = "00001101-0000-1000-8000-00805f9b34fb";
        private BluetoothAdapter _adapter;
        private BluetoothSocket _socket;

    }



    public static class MicIn
    {
        static MediaRecorder mRecorder;
        public static double amplitude;
        private static double mEMA = 0.0;
        private const double EMA_FILTER = 0.6;

        public static float measureSoundLevel()
        {
            return 0f;
        }

        public static double convertdDb(double amplitude)
        {
            // Cellphones can catch up to 90 db + -
            // getMaxAmplitude returns a value between 0-32767 (in most phones). that means that if the maximum db is 90, the pressure
            // at the microphone is 0.6325 Pascal.
            // it does a comparison with the previous value of getMaxAmplitude.
            // we need to divide maxAmplitude with (32767/0.6325)
            //51805.5336 or if 100db so 46676.6381
            double EMA_FILTER = 0.6;
            double mEMAValue = EMA_FILTER * amplitude + (1.0 - EMA_FILTER) * mEMA;
            //Assuming that the minimum reference pressure is 0.000085 Pascal (on most phones) is equal to 0 db
            // samsung S9 0.000028251
            return 20 * (float)Math.Log10((mEMAValue / 51805.5336) / 0.000028251);
        }

        public static double getAmplitudeEMA()
        {
            double amp = getAmplitude();
            mEMA = EMA_FILTER * amp + (1.0 - EMA_FILTER) * mEMA;
            return mEMA;
        }

        public static double getAmplitude()
        {
            if (mRecorder != null)
                return (mRecorder.MaxAmplitude);
            else
                return 0;

        }

        public static void startRecorder()
        {
            
            Java.IO.File file = new Java.IO.File(FileSystem.CacheDirectory + "/" + Java.IO.File.Separator + "test.3gp");
            file.CreateNewFile();


            if (mRecorder == null)
            {
                mRecorder = new MediaRecorder();
                mRecorder.SetAudioSource(AudioSource.Mic);
                mRecorder.SetOutputFormat(OutputFormat.ThreeGpp);
                mRecorder.SetAudioEncoder(AudioEncoder.AmrNb);
                mRecorder.SetOutputFile($"{FileSystem.CacheDirectory}/test.3gp");

                mRecorder.Prepare();
                /*catch (Java.IO.IOException ioe)
                {
                    Debug.WriteLine("IO exception");

                }
                catch (Java.Lang.SecurityException e)
                {
                    Debug.WriteLine("Security exception");
                }*/
                try
                {
                    mRecorder.Start();
                }
                catch (Java.Lang.IllegalStateException e)
                {
                    Debug.WriteLine("Illegal state exception");
                }

                //mEMA = 0.0;
            }

        }

    }   
}
#endif

