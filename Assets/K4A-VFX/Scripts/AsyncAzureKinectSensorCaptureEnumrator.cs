using System.Threading;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using Microsoft.Azure.Kinect.Sensor;
using UnityEngine;

namespace K4A.VFX
{
    public class AsyncAzureKinectSensorCaptureEnumrator : IUniTaskAsyncEnumerator<Capture>
    {
        private Device _kinect;

        private CancellationToken token;

        public Capture Current { get; private set; }

        public AsyncAzureKinectSensorCaptureEnumrator(Device kinect, CancellationToken token)
        {
            this._kinect = kinect;
            this.token = token;
            Current = null;
        }

        public async UniTask DisposeAsync()
        {
            Debug.Log("dispose in enumerator.");
            await UniTask.Run(() =>
            {
                Current.Dispose();
                _kinect.StopCameras();
                _kinect.Dispose();
            }, true, token);
        }

        public async UniTask<bool> MoveNextAsync()
        {
            return await UniTask.Run(() =>
            {
                Debug.Log("move next");
                Current = _kinect.GetCapture();
                return Current != null;
            }, true, token);
        }
    }
}