using System;
using System.Threading;
using UnityEngine;
using Microsoft.Azure.Kinect.Sensor;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using Cysharp.Threading;
using Cysharp.Threading.Tasks.Linq;
using ICSharpCode.NRefactory.Ast;

namespace K4A.VFX
{
    public class AzureKinectSensor : MonoBehaviour
    {
        private Device kinect;

        private CancellationTokenSource cts;

        private void Start()
        {
            kinect = Device.Open();
            kinect.StartCameras(new DeviceConfiguration
            {
                ColorFormat = ImageFormat.ColorBGRA32,
                ColorResolution = ColorResolution.R1080p,
                DepthMode = DepthMode.NFOV_2x2Binned,
                SynchronizedImagesOnly = true,
                CameraFPS = FPS.FPS30
            });
            
            cts = new CancellationTokenSource();

            var ctn = cts.Token;

            var sensor = new AsyncAzureKinectSensorCaptureEnumerable(kinect, ctn);
            sensor.ForEachAsync((capture, token) =>
            {
                Debug.Log(capture.Color.DeviceTimestamp.TotalSeconds);
            }, ctn);
            

        }

        private void OnApplicationQuit()
        {
            Debug.Log("dispose in monobehavior");
            cts.Cancel();
            if (kinect != null)
            {
                kinect.StopCameras();
                kinect.Dispose();
            }
        }
    }
}