#if !UNITY_WSA_10_0

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.DnnModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UnityUtils.Helper;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Human Segmentation PPHumanSeg Example
    /// An example of using OpenCV dnn module with Human Segmentation model.
    /// Referring to https://github.com/opencv/opencv_zoo/tree/master/models/human_segmentation_pphumanseg
    /// </summary>
    [RequireComponent(typeof(WebCamTextureToMatHelper))]
    public class StandaloneHumanSegmentation : MonoBehaviour
    {
        public AppFlowManager AppFlowManager;
        /// <summary>
        /// The compose bg image toggle.
        /// </summary>
        public Toggle composeBGImageToggle;

        /// <summary>
        /// The hide person toggle.
        /// </summary>
        public Toggle hidePersonToggle;

        /// <summary>
        /// The background image texture.
        /// </summary>
        public Texture2D backGroundImageTexture;

        /// <summary>
        /// The texture.
        /// </summary>
        Texture2D texture;

        /// <summary>
        /// The webcam texture to mat helper.
        /// </summary>
        WebCamTextureToMatHelper webCamTextureToMatHelper;

        /// <summary>
        /// The rgb mat.
        /// </summary>
        Mat rgbMat;

        /// <summary>
        /// The mask mat.
        /// </summary>
        Mat maskMat;

        /// <summary>
        /// The background mask mat.
        /// </summary>
        Mat bgMaskMat;

        /// <summary>
        /// The background image mat.
        /// </summary>
        Mat backGroundImageMat;

        /// <summary>
        /// The net.
        /// </summary>
        Net net;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        FpsMonitor fpsMonitor;

        /// <summary>
        /// MODEL_FILENAME
        /// </summary>
        protected static readonly string MODEL_FILENAME = "OpenCVForUnity/dnn/human_segmentation_pphumanseg_2023mar.onnx";

        /// <summary>
        /// The model filepath.
        /// </summary>
        string model_filepath;

#if UNITY_WEBGL
        IEnumerator getFilePath_Coroutine;
#endif

        // Use this for initialization
        void Start()
        {
            fpsMonitor = GetComponent<FpsMonitor>();

            webCamTextureToMatHelper = gameObject.GetComponent<WebCamTextureToMatHelper>();

#if UNITY_WEBGL
            getFilePath_Coroutine = GetFilePath();
            StartCoroutine(getFilePath_Coroutine);
#else
            model_filepath = Utils.getFilePath(MODEL_FILENAME);
            Run();
#endif
        }

#if UNITY_WEBGL
        private IEnumerator GetFilePath()
        {
            var getFilePathAsync_0_Coroutine = Utils.getFilePathAsync(MODEL_FILENAME, (result) =>
            {
                model_filepath = result;
            });
            yield return getFilePathAsync_0_Coroutine;

            getFilePath_Coroutine = null;

            Run();
        }
#endif

        // Use this for initialization
        void Run()
        {
            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            Utils.setDebugMode(true);

            if (string.IsNullOrEmpty(model_filepath))
            {
                Debug.LogError(MODEL_FILENAME + " is not loaded. Please read “StreamingAssets/OpenCVForUnity/dnn/setup_dnn_module.pdf” to make the necessary setup.");
            }
            else
            {
                net = Dnn.readNet(model_filepath);
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            // Avoids the front camera low light issue that occurs in only some Android devices (e.g. Google Pixel, Pixel2).
            webCamTextureToMatHelper.avoidAndroidFrontCameraLowLightIssue = true;
#endif
            webCamTextureToMatHelper.Initialize();
        }

        /// <summary>
        /// Raises the webcam texture to mat helper initialized event.
        /// </summary>
        public void OnWebCamTextureToMatHelperInitialized()
        {
            Debug.Log("OnWebCamTextureToMatHelperInitialized");

            Mat webCamTextureMat = webCamTextureToMatHelper.GetMat();

            texture = new Texture2D(webCamTextureMat.cols(), webCamTextureMat.rows(), TextureFormat.RGBA32, false);
            Utils.matToTexture2D(webCamTextureMat, texture);

            gameObject.GetComponent<Renderer>().material.mainTexture = texture;

            gameObject.transform.localScale = new Vector3(webCamTextureMat.cols(), webCamTextureMat.rows(), 1);
            Debug.Log("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);

            if (fpsMonitor != null)
            {
                fpsMonitor.Add("width", webCamTextureMat.width().ToString());
                fpsMonitor.Add("height", webCamTextureMat.height().ToString());
                fpsMonitor.Add("orientation", Screen.orientation.ToString());
            }

            float width = webCamTextureMat.width();
            float height = webCamTextureMat.height();

            float widthScale = (float)Screen.width / width;
            float heightScale = (float)Screen.height / height;
            if (widthScale < heightScale)
            {
                Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
            }
            else
            {
                Camera.main.orthographicSize = height / 2;
            }

            rgbMat = new Mat(webCamTextureMat.rows(), webCamTextureMat.cols(), CvType.CV_8UC3);
            maskMat = new Mat(webCamTextureMat.rows(), webCamTextureMat.cols() * 2, CvType.CV_8UC1);

            bgMaskMat = new Mat(webCamTextureMat.rows(), webCamTextureMat.cols(), CvType.CV_8UC1);
            backGroundImageMat = new Mat(webCamTextureMat.size(), CvType.CV_8UC4, new Scalar(39, 255, 86, 255));
            if (backGroundImageTexture != null)
            {
                using (Mat bgMat = new Mat(backGroundImageTexture.height, backGroundImageTexture.width, CvType.CV_8UC4))
                {
                    Utils.texture2DToMat(backGroundImageTexture, bgMat);
                    Imgproc.resize(bgMat, backGroundImageMat, backGroundImageMat.size());
                }
            }
        }

        /// <summary>
        /// Raises the webcam texture to mat helper disposed event.
        /// </summary>
        public void OnWebCamTextureToMatHelperDisposed()
        {
            Debug.Log("OnWebCamTextureToMatHelperDisposed");

            if (rgbMat != null)
                rgbMat.Dispose();

            if (maskMat != null)
                maskMat.Dispose();

            if (bgMaskMat != null)
                bgMaskMat.Dispose();

            if (backGroundImageMat != null)
                backGroundImageMat.Dispose();

            if (texture != null)
            {
                Texture2D.Destroy(texture);
                texture = null;
            }
        }

        /// <summary>
        /// Raises the webcam texture to mat helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        public void OnWebCamTextureToMatHelperErrorOccurred(WebCamTextureToMatHelper.ErrorCode errorCode)
        {
            Debug.Log("OnWebCamTextureToMatHelperErrorOccurred " + errorCode);
        }

        // Update is called once per frame
        void Update()
        {
            if (webCamTextureToMatHelper.IsPlaying() && webCamTextureToMatHelper.DidUpdateThisFrame())
            {

                Mat rgbaMat = webCamTextureToMatHelper.GetMat();

                if (net == null)
                {
                    Imgproc.putText(rgbaMat, "model file is not loaded.", new Point(5, rgbaMat.rows() - 30), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                    Imgproc.putText(rgbaMat, "Please read console message.", new Point(5, rgbaMat.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                }
                else
                {

                    Imgproc.cvtColor(rgbaMat, rgbMat, Imgproc.COLOR_RGBA2RGB);


                    Mat blob = Dnn.blobFromImage(rgbMat, 1.0 / 255.0, new Size(192, 192), new Scalar(0.5, 0.5, 0.5), false, false, CvType.CV_32F);
                    // Divide blob by std.
                    Core.divide(blob, new Scalar(0.5, 0.5, 0.5), blob);


                    net.setInput(blob);

                    Mat prob = net.forward("save_infer_model/scale_0.tmp_1");

                    Mat result = new Mat();
                    Core.reduceArgMax(prob, result, 1);
                    //result.reshape(0, new int[] { 192,192});
                    result.convertTo(result, CvType.CV_8U, 255.0);
                    //Debug.Log("result.ToString(): " + result.ToString());

                    Mat mask192x192 = new Mat(192, 192, CvType.CV_8UC1, (IntPtr)result.dataAddr());
                    Imgproc.resize(mask192x192, maskMat, rgbaMat.size(), Imgproc.INTER_LANCZOS4);
                    
                    // Edge blurring
                    // Blur the mask to smooth the edges
                    Imgproc.GaussianBlur(maskMat, maskMat, new Size(5, 5), 0);

                    // Apply a threshold to create a binary mask
                    Imgproc.threshold(maskMat, maskMat, 127, 255, Imgproc.THRESH_BINARY);

                    // Define the structuring element for morphological operations
                    Mat kernel = Imgproc.getStructuringElement(Imgproc.MORPH_RECT, new Size(3, 3));

                    // Apply morphological operations to refine the edges
                    Imgproc.morphologyEx(maskMat, maskMat, Imgproc.MORPH_CLOSE, kernel);
                    Imgproc.morphologyEx(maskMat, maskMat, Imgproc.MORPH_OPEN, kernel);

                    // Release the kernel
                    kernel.Dispose();
                    
                    List<MatOfPoint> contours = new List<MatOfPoint>();
                    Mat hierarchy = new Mat();
                    Imgproc.findContours(maskMat, contours, hierarchy, Imgproc.RETR_EXTERNAL, Imgproc.CHAIN_APPROX_SIMPLE);
                    Imgproc.drawContours(maskMat, contours, -1, new Scalar(255, 255, 255), 2);

                    if (composeBGImageToggle.isOn)
                    {
                        // Compose the background image.
                        Core.bitwise_not(maskMat, bgMaskMat);
                        //Imgproc.bilateralFilter(bgMaskMat,maskMat,9,150,150, Core.BORDER_WRAP);
                        backGroundImageMat.copyTo(rgbaMat, bgMaskMat);
                    }

                    if (hidePersonToggle.isOn)
                    {
                        rgbaMat.setTo(new Scalar(255, 255, 255, 255), maskMat);
                    }
                    
                    bool isHumanNearCenter = false;
                    
                    if (maskMat != null)
                    {
                        // Calculate the center of the image
                        int centerX = maskMat.cols() / 2;
                        int centerY = maskMat.rows() / 2;

                        // Define a threshold for "near center" (adjust as needed)
                        int nearCenterThreshold = 50;

                        // Iterate through a specific region around the center
                        for (int y = centerY - nearCenterThreshold; y < centerY + nearCenterThreshold; y++)
                        {
                            for (int x = centerX - nearCenterThreshold; x < centerX + nearCenterThreshold; x++)
                            {
                                byte[] pixelValue = new byte[1];
                                maskMat.get(y, x, pixelValue);
                                // Check if the pixel value in the mask indicates a human (value might be 1 or 255 depending on implementation)
                                if (pixelValue[0] > 0)
                                {
                                    isHumanNearCenter = true;
                                    break; // Exit loop once a human is found
                                }
                            }
                        }
                    }

                    AppFlowManager.FaceDetected = isHumanNearCenter;

                    mask192x192.Dispose();
                    result.Dispose();

                    prob.Dispose();
                    blob.Dispose();

                }

                Utils.matToTexture2D(rgbaMat, texture);
            }
        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy()
        {
            webCamTextureToMatHelper.Dispose();

            if (net != null)
                net.Dispose();

            Utils.setDebugMode(false);

#if UNITY_WEBGL
            if (getFilePath_Coroutine != null)
            {
                StopCoroutine(getFilePath_Coroutine);
                ((IDisposable)getFilePath_Coroutine).Dispose();
            }
#endif
        }

        /// <summary>
        /// Raises the back button click event.
        /// </summary>
        public void OnBackButtonClick()
        {
            SceneManager.LoadScene("OpenCVForUnityExample");
        }

        /// <summary>
        /// Raises the play button click event.
        /// </summary>
        public void OnPlayButtonClick()
        {
            webCamTextureToMatHelper.Play();
        }

        /// <summary>
        /// Raises the pause button click event.
        /// </summary>
        public void OnPauseButtonClick()
        {
            webCamTextureToMatHelper.Pause();
        }

        /// <summary>
        /// Raises the stop button click event.
        /// </summary>
        public void OnStopButtonClick()
        {
            webCamTextureToMatHelper.Stop();
        }

        /// <summary>
        /// Raises the change camera button click event.
        /// </summary>
        public void OnChangeCameraButtonClick()
        {
            webCamTextureToMatHelper.requestedIsFrontFacing = !webCamTextureToMatHelper.requestedIsFrontFacing;
        }
    }
}

#endif