using System;
using System.Collections;
using System.Linq;
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

        private Color32[] _colors;

        private static readonly int MainTex = Shader.PropertyToID("_BaseColorMap");

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

            UniTaskAsyncEnumerable.Create<BGRA[]>(async (writer, token) =>
                {
                    await UniTask.Run(() =>
                    {
                        while (isRunning)
                        {
                            using (var capture = kinect.GetCapture())
                            {
                                writer.YieldAsync(capture.Color.GetPixels<BGRA>().ToArray());
                            }
                        }
                    }, cancellationToken: token);
                })
                .Select(arr =>
                {
                    return arr.Select(bgra =>
                            new Color32(bgra.R, bgra.G, bgra.B, 255))
                        .ToArray();
                })
                .ForEachAwaitWithCancellationAsync(
                    async (colors, token) =>
                    {
                        await UniTask.Run(() => { _colors = colors; }, cancellationToken: token);
                    }, this.GetCancellationTokenOnDestroy());
        }

        private void Update()
        {
            if (_colors != null)
            {
                var width = kinect.GetCalibration().ColorCameraCalibration.ResolutionWidth;
                var height = kinect.GetCalibration().ColorCameraCalibration.ResolutionHeight;
                if (_colors.Length == width * height)
                {
                    var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                    texture.SetPixels32(0, 0, width, height, _colors);
                    texture.Apply();
                    Destroy(_material.GetTexture(MainTex));
                    _material.SetTexture(MainTex, texture);
                }
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