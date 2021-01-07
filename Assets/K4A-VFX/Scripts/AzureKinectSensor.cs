using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Microsoft.Azure.Kinect.Sensor;
using UnityEngine.Serialization;
using UnityEngine.VFX;

namespace K4A.VFX
{
    public class AzureKinectSensor : MonoBehaviour
    {
        private Device _kinect;

        private bool _isRunning = false;

        private byte[] _rawColorData = null;
        private Color[] _xyzs = null;

        private static readonly int MainTex = Shader.PropertyToID("_BaseColorMap");

        private CameraCalibration _depthCameraCalibration;
        private Transformation _kinectTransformation;

        private Texture2D _colorTexture2D;
        private Texture2D _depthTexture2D;

        [SerializeField] private VisualEffect effect;

        private readonly int _propertyWidth = Shader.PropertyToID("width");
        private readonly int _propertyHeight = Shader.PropertyToID("height");
        private readonly int _propertyColorImage = Shader.PropertyToID("colorImage");
        private readonly int _propertyXyzImage = Shader.PropertyToID("xyzImage");

        [SerializeField] private GameObject previewPlane;

        private void Start()
        {
            _kinect = Device.Open();
            _kinect.StartCameras(new DeviceConfiguration
            {
                ColorFormat = ImageFormat.ColorBGRA32,
                ColorResolution = ColorResolution.R1080p,
                DepthMode = DepthMode.NFOV_2x2Binned,
                SynchronizedImagesOnly = true,
                CameraFPS = FPS.FPS30
            });

            _isRunning = true;

            // get calibrations
            _depthCameraCalibration = _kinect.GetCalibration().DepthCameraCalibration;
            _kinectTransformation = _kinect.GetCalibration().CreateTransformation();

            // texture settings
            _colorTexture2D = new Texture2D(_depthCameraCalibration.ResolutionWidth,
                _depthCameraCalibration.ResolutionHeight, TextureFormat.BGRA32, false);

            _depthTexture2D = new Texture2D(_depthCameraCalibration.ResolutionWidth,
                _depthCameraCalibration.ResolutionHeight, TextureFormat.RGBAFloat, false)
            {
                filterMode = FilterMode.Point
            };

            // preview panel - plane object for preview color/depth texture
            if (previewPlane != null)
            {
                previewPlane.GetComponent<MeshRenderer>().material.SetTexture(MainTex, _colorTexture2D);
                previewPlane.transform.localScale = new Vector3(1f, 1f,
                    (float) _depthCameraCalibration.ResolutionHeight / _depthCameraCalibration.ResolutionWidth);
            }

            // vfx settings
            if (effect != null)
            {
                effect.SetUInt(_propertyWidth, (uint) _depthCameraCalibration.ResolutionWidth);
                effect.SetUInt(_propertyHeight, (uint) _depthCameraCalibration.ResolutionHeight);
                effect.SetTexture(_propertyColorImage, _colorTexture2D);
                effect.SetTexture(_propertyXyzImage, _depthTexture2D);
            }

            UniTask.Run(() =>
            {
                while (_isRunning)
                {
                    using (var capture = _kinect.GetCapture())
                    {
                        Image colorImage = _kinectTransformation.ColorImageToDepthCamera(capture);

                        _rawColorData = colorImage.Memory.ToArray();

                        Image xyzImage = _kinectTransformation.DepthImageToPointCloud(capture.Depth);
                        _xyzs = xyzImage.GetPixels<Short3>()
                            .ToArray()
                            .Select(short3 => new Color(short3.X / 100.0f, -short3.Y / 100.0f, short3.Z / 100.0f))
                            .ToArray();
                    }
                }
            }, true, this.GetCancellationTokenOnDestroy()).Forget();
        }

        private void Update()
        {
            if (_rawColorData != null)
            {
                _colorTexture2D.LoadRawTextureData(_rawColorData);
                _colorTexture2D.Apply();
            }

            if (_xyzs != null)
            {
                _depthTexture2D.SetPixels(_xyzs);
                _depthTexture2D.Apply();
            }
        }


        private void OnApplicationQuit()
        {
            _isRunning = false;
            if (_kinect != null)
            {
                _kinect.StopCameras();
                _kinect.Dispose();
            }
        }
    }
}