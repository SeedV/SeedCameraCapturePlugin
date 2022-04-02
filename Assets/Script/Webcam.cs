using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SeedFFmpeg
{
    public class Webcam : MonoBehaviour
    {
        public string _device;
        private WebCamTexture _webcam;
        [SerializeField] private RenderTexture _buffer;
        [SerializeField] string _rtmp_url;


        [SerializeField] int _width = 1280;

        public int width
        {
            get { return _width; }
            set { _width = value; }
        }

        [SerializeField] int _height = 720;

        public int height
        {
            get { return _height; }
            set { _height = value; }
        }

        [SerializeField] float _frameRate = 60;

        public float frameRate
        {
            get { return _frameRate; }
            set { _frameRate = value; }
        }

        FFmpegPreset _preset;

        public FFmpegPreset preset
        {
            get { return _preset; }
            set { _preset = value; }
        }


        FFmpegSession _session;
        RenderTexture _tempRT;
        GameObject _blitter;

        RenderTextureFormat GetTargetFormat(Camera camera)
        {
            return camera.allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;
        }

        int GetAntiAliasingLevel(Camera camera)
        {
            return camera.allowMSAA ? QualitySettings.antiAliasing : 1;
        }


        int _frameCount;
        float _startTime;
        int _frameDropCount;

        float FrameTime
        {
            get { return _startTime + (_frameCount - 0.5f) / _frameRate; }
        }

        void WarnFrameDrop()
        {
            if (++_frameDropCount != 10) return;

            Debug.LogWarning(
                "Significant frame droppping was detected. This may introduce " +
                "time instability into output video. Decreasing the recording " +
                "frame rate is recommended."
            );
        }






        // Start is called before the first frame update
        IEnumerator Start()
        {
            _rtmp_url = "rtmp://localhost/live/test";
            yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
            if (Application.HasUserAuthorization(UserAuthorization.WebCam))
            {
                WebCamDevice[] devices = WebCamTexture.devices;
                _device = devices[2].name;
                _webcam = new WebCamTexture(_device, 1920, 1080, 30);
                //GetComponent<Renderer>().material.mainTexture = _webcam;
                _webcam.Play();
            }
            


            for (var eof = new WaitForEndOfFrame(); ;)
            {
                yield return eof;
                _session?.CompletePushFrames();
            }








        }

        // Update is called once per frame
        void Update()
        {
            Vector2 scale = new Vector2(-1, 1);
            Vector2 offset = new Vector2(1, 0);
            Graphics.Blit(_webcam, _buffer, scale, offset);


            var camera = GetComponent<Camera>();

            // Lazy initialization
            if (_session == null)
            {
                // Give a newly created temporary render texture to the camera
                // if it's set to render to a screen. Also create a blitter
                // object to keep frames presented on the screen.
                if (camera.targetTexture == null)
                {
                    _tempRT = new RenderTexture(_width, _height, 24, GetTargetFormat(camera));
                    _tempRT.antiAliasing = GetAntiAliasingLevel(camera);
                    camera.targetTexture = _tempRT;
                    _blitter = Blitter.CreateInstance(camera);
                }

                // Start an FFmpeg session.
                _session = FFmpegSession.Create(
                    gameObject.name,
                    _width,
                    _height,
                    _frameRate,
                    preset,
                    _rtmp_url
                );

                _startTime = Time.time;
                _frameCount = 0;
                _frameDropCount = 0;
            }

            var gap = Time.time - FrameTime;
            var delta = 1 / _frameRate;

            if (gap < 0)
            {
                //Debug.Log("push null frame");
                // Update without frame data
                _session.PushFrame(null);
            }
            else if (gap < delta)
            {
                //Debug.Log("push 1 frame");
                // Single-frame behind from the current time:
                // Push the current frame to FFmpeg.
                _session.PushFrame(camera.targetTexture);
                _frameCount++;
            }
            else if (gap < delta * 2)
            {
                //Debug.Log("push 2 frame");
                // Two-frame behind from the current time:
                // Push the current frame twice to FFmpeg. Actually this is not
                // an efficient way to catch up. We should think about
                // implementing frame duplication in a more proper way. #fixme
                _session.PushFrame(camera.targetTexture);
                _session.PushFrame(camera.targetTexture);
                _frameCount += 2;
            }
            else
            {
                // Show a warning message about the situation.
                WarnFrameDrop();

                // Push the current frame to FFmpeg.
                _session.PushFrame(camera.targetTexture);

                // Compensate the time delay.
                _frameCount += Mathf.FloorToInt(gap * _frameRate);
            }
        }

        void OnDisable()
        {
            if (_session != null)
            {
                // Close and dispose the FFmpeg session.
                _session.Close();
                _session.Dispose();
                _session = null;
            }

            if (_tempRT != null)
            {
                // Dispose the frame texture.
                GetComponent<Camera>().targetTexture = null;
                Destroy(_tempRT);
                _tempRT = null;
            }

            if (_blitter != null)
            {
                // Destroy the blitter game object.
                Destroy(_blitter);
                _blitter = null;
            }
        }
    }
}


