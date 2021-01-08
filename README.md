# k4a-vfx

## About

Azure Kinectの点群をUnity Visual Effect Graphで使うサンプルプロジェクト。  
表現の幅を考えてHDRPを採用しています。動作にはGPUを積んだWindwosデスクトップPCが推奨です。

## Tested Environment

||環境|
|:---:|:---:|
|OS|Windows 10 Home|
|Unity|2019.4.16|
|Render Pipeline|HDRP 7.|
|Azure Kinect Sensor SDK|1.4.1|
|GPU|NVIDIA GeForce GTX 1060 3GB|

## Setup & Usage

### Using Visual Studio

リポジトリを適当なディレクトリへclone後、ソリューションファイル(`k4a-vfx.sln`)VisualStudioで開きます。
ソリューションファイルが見つからない場合は、Unityでプロジェクトを開いてから、メニューバーのAssets->Open C# ProjectからVisual Studioを開きます。
そしたらソリューションウィンドウのソリューションを右クリックして「nugetパッケージを復元」を選択し、Visual Studioを閉じます。
最後に`movePackages.bat`を実行し、Unityプロジェクトを開きます。

### Using NuGet CLI

プロジェクトのルートディレクトリで以下のコマンドを実行します。

```bash
# install nuget packages
$ nuget install packages.config -o ./External/Packages

# copy dlls from nuget packages
$ ./movePackages.bat
```

そのあとにUnityプロジェクトを開きます。

## Contact 
