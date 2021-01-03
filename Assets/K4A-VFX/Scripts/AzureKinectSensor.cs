using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using UnityEngine;
using Microsoft.Azure.Kinect.Sensor;
using UnityEngine.PlayerLoop;

namespace K4A.VFX
{
    public class AzureKinectSensor : MonoBehaviour
    {
        private Device kinect;

        private bool isRunning = false;

        private Material _material;

        private byte[] _rawTexture;

        private static readonly int MainTex = Shader.PropertyToID("_BaseColorMap");

        private Color32[] _colors;

        private CameraCalibration _colorCameraCalibration;


        private void Start()
        {
            _material = GetComponent<MeshRenderer>().material;

            kinect = Device.Open();
            kinect.StartCameras(new DeviceConfiguration
            {
                ColorFormat = ImageFormat.ColorBGRA32,
                ColorResolution = ColorResolution.R1080p,
                DepthMode = DepthMode.NFOV_2x2Binned,
                SynchronizedImagesOnly = true,
                CameraFPS = FPS.FPS30
            });

            isRunning = true;

            _colorCameraCalibration = kinect.GetCalibration().ColorCameraCalibration;


            // UniTaskAsyncEnumerable.Create<BGRA[]>(async (writer, token) =>
            //     {
            //         await UniTask.Run(() =>
            //         {
            //             while (isRunning)
            //                 using (var capture = kinect.GetCapture())
            //                     writer.YieldAsync(capture.Color.GetPixels<BGRA>().ToArray());
            //         }, cancellationToken: token);
            //     })
            // .Select(arr =>
            // {
            //     return arr.Select(bgra =>
            //             new Color32(bgra.R, bgra.G, bgra.B, bgra.A))
            //         .ToArray();
            // })
            // .ForEachAsync(
            //     (colors, token) => { _colors = colors; }, this.GetCancellationTokenOnDestroy());
            //


            // UniTaskAsyncEnumerable.Create<byte[]>(async (writer, token) =>
            // {
            //     await UniTask.Run(() =>
            //     {
            //         while (isRunning)
            //         {
            //             using (var capture = kinect.GetCapture())
            //             {
            //                 writer.YieldAsync(capture.Color.Memory.ToArray());
            //             }
            //         }
            //     }, cancellationToken: token);
            // }).ForEachAsync(bytes => { _rawTexture = bytes; }, this.GetCancellationTokenOnDestroy());


            // KinectLoop();

            UniTask.Run(() =>
            {
                while (isRunning)
                {
                    using (var capture = kinect.GetCapture())
                    {
                        _rawTexture = capture.Color.Memory.ToArray();
                    }
                }
            }, true, this.GetCancellationTokenOnDestroy()).Forget();
        }

        async void KinectLoop()
        {
            // while (isRunning)
            // {
            //     using (var capture = await Task.Run(() => kinect.GetCapture()))
            //     {
            //         // var bgraImage = capture.Color.GetPixels<BGRA>().ToArray();
            //         // _colors = bgraImage.Select(bgra => new Color32(bgra.R, bgra.G, bgra.B, bgra.A)).ToArray();
            //         _rawTexture = capture.Color.Memory.ToArray();
            //     }
            // }

            await Task.Run(() =>
            {
                while (isRunning)
                {
                    using (var capture = kinect.GetCapture())
                    {
                        // var bgraImage = capture.Color.GetPixels<BGRA>().ToArray();
                        // _colors = bgraImage.Select(bgra => new Color32(bgra.R, bgra.G, bgra.B, bgra.A)).ToArray();
                        try
                        {
                            _rawTexture = capture.Color.Memory.ToArray();
                        }
                        catch (Exception e)
                        {
                            Debug.Log(e);
                            throw;
                        }
                    }
                }
            });
        }

        private void Update()
        {
            if (_rawTexture != null)
            {
                var texture2D = new Texture2D(_colorCameraCalibration.ResolutionWidth,
                    _colorCameraCalibration.ResolutionHeight, TextureFormat.BGRA32, false);

                texture2D.LoadRawTextureData(_rawTexture);
                texture2D.Apply();
                Destroy(_material.GetTexture(MainTex));
                _material.SetTexture(MainTex, texture2D);
            }
        }


        private void OnApplicationQuit()
        {
            isRunning = false;
            if (kinect != null)
            {
                kinect.StopCameras();
                kinect.Dispose();
            }
        }
    }
}