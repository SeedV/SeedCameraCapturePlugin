# SeedCameraCapturePlugin
CameraCapturePlugin for Unity


## Setup

YThe recommended Unity editor version is 2020.3.30f1 LTS, but other editor may likely work.

Check out the project files into a local directory and open Unity editor. Then based on the platform, please rebuild FFmpeg 4.x from https://github.com/FFmpeg/FFmpeg

Copy the built files into Assets/Plugin/FFmpeg/Linux&&macOS&&Windows  



## Description

Capture Camera frame, Use FFmpeg encoder to h264 flv and push rtmp server

You can run a rtmp server with https://github.com/iizukanao/node-rtsp-rtmp-server

and use VLC player get stream 






## License

SeedCameraCapturePlugin is under LGPL-licensed.

FFmpeg codebase is mainly LGPL-licensed with optional components licensed under
GPL. Please refer to the LICENSE file for detailed information.
