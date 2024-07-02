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
using OpenCVForUnity.ObjdetectModule;
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
    public class HumanSegmentationPPHumanSegExample : MonoBehaviour
    {
        // Human Segmentation
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
        private WebCamTextureToMatHelper webCamTextureToMatHelper;

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
        
        // Face Detection
        
        [SerializeField] private AppFlowManager _appFlowManager;
        
        /// <summary>
        /// The gray mat.
        /// </summary>
        Mat grayMat;
        
        /// <summary>
        /// The cascade.
        /// </summary>
        CascadeClassifier cascade;
        
        /// <summary>
        /// The faces.
        /// </summary>
        MatOfRect faces;
        
        protected static readonly string LBP_CASCADE_FILENAME = "OpenCVForUnity/objdetect/lbpcascade_frontalface.xml";
        

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
            
            // Face Detection
            string cascade_filepath = Utils.getFilePath(LBP_CASCADE_FILENAME);
            if (string.IsNullOrEmpty(cascade_filepath))
            {
                Debug.LogError(LBP_CASCADE_FILENAME + " is not loaded. Please move from “OpenCVForUnity/StreamingAssets/OpenCVForUnity/” to “Assets/StreamingAssets/OpenCVForUnity/” folder.");
            }
            else
            {
                cascade = new CascadeClassifier(cascade_filepath);
            }
            
            // Human Segmentation
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
            
            // Face Detecion
            
            Mat webCamTextureMat_1 = webCamTextureToMatHelper.GetMat();

            texture = new Texture2D(webCamTextureMat_1.cols(), webCamTextureMat_1.rows(), TextureFormat.RGBA32, false);
            Utils.matToTexture2D(webCamTextureMat_1, texture);

            gameObject.GetComponent<Renderer>().material.mainTexture = texture;

            gameObject.transform.localScale = new Vector3(webCamTextureMat_1.cols(), webCamTextureMat_1.rows(), 1);
            Debug.Log("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);

            if (fpsMonitor != null)
            {
                fpsMonitor.Add("width", webCamTextureMat_1.width().ToString());
                fpsMonitor.Add("height", webCamTextureMat_1.height().ToString());
                fpsMonitor.Add("orientation", Screen.orientation.ToString());
            }

            float width_1 = webCamTextureMat_1.width();
            float height_1 = webCamTextureMat_1.height();

            float widthScale_1 = (float)Screen.width / width_1;
            float heightScale_1 = (float)Screen.height / height_1;
            if (widthScale_1 < heightScale_1)
            {
                Camera.main.orthographicSize = (width_1 * (float)Screen.height / (float)Screen.width) / 2;
            }
            else
            {
                Camera.main.orthographicSize = height_1 / 2;
            }

            grayMat = new Mat(webCamTextureMat_1.rows(), webCamTextureMat_1.cols(), CvType.CV_8UC1);

            faces = new MatOfRect();
            
            
            // Human Segmentation

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
            maskMat = new Mat(webCamTextureMat.rows(), webCamTextureMat.cols(), CvType.CV_8UC1);

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

            // Human Segmentation
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
            
            // Face Detection
            if (grayMat != null)
                grayMat.Dispose();
            
            if (faces != null)
                faces.Dispose();
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
                // Face Detection
                Mat rgbaMat_1 = webCamTextureToMatHelper.GetMat();

                if (cascade == null)
                {
                    Imgproc.putText(rgbaMat_1, "model file is not loaded.", new Point(5, rgbaMat_1.rows() - 30), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                    Imgproc.putText(rgbaMat_1, "Please read console message.", new Point(5, rgbaMat_1.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

                    Utils.matToTexture2D(rgbaMat_1, texture);
                    return;
                }


                Imgproc.cvtColor(rgbaMat_1, grayMat, Imgproc.COLOR_RGBA2GRAY);
                Imgproc.equalizeHist(grayMat, grayMat);


                if (cascade != null)
                    cascade.detectMultiScale(grayMat, faces, 1.1, 2, 2, // TODO: objdetect.CV_HAAR_SCALE_IMAGE
                        new Size(grayMat.cols() * 0.2, grayMat.rows() * 0.2), new Size());


                OpenCVForUnity.CoreModule.Rect[] rects = faces.toArray();

                _appFlowManager.FaceFound = rects.Length == 1;
                
                for (int i = 0; i < rects.Length; i++)
                {
                    //Debug.Log ("detect faces " + rects [i]);
                    //Imgproc.rectangle(rgbaMat, new Point(rects[i].x, rects[i].y), new Point(rects[i].x + rects[i].width, rects[i].y + rects[i].height), new Scalar(255, 0, 0, 255), 2);
                }

                //Imgproc.putText (rgbaMat, "W:" + rgbaMat.width () + " H:" + rgbaMat.height () + " SO:" + Screen.orientation, new Point (5, rgbaMat.rows () - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

                Utils.matToTexture2D(rgbaMat_1, texture);
                
                
                
                // Human Segmentation
                Mat rgbaMat_2 = webCamTextureToMatHelper.GetMat();

                if (net == null)
                {
                    Imgproc.putText(rgbaMat_2, "model file is not loaded.", new Point(5, rgbaMat_2.rows() - 30), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                    Imgproc.putText(rgbaMat_2, "Please read console message.", new Point(5, rgbaMat_2.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                }
                else
                {

                    Imgproc.cvtColor(rgbaMat_2, rgbMat, Imgproc.COLOR_RGBA2RGB);


                    Mat blob = Dnn.blobFromImage(rgbMat, 1.0f / 255.0f, new Size(192, 192), new Scalar(0.5, 0.5, 0.5), false, false, CvType.CV_32F);
                    // Divide blob by std.
                    Core.divide(blob, new Scalar(0.5, 0.5, 0.5), blob);


                    net.setInput(blob);

                    Mat prob = net.forward("save_infer_model/scale_0.tmp_1");

                    Mat result = new Mat();
                    Core.reduceArgMax(prob, result, 1);
                    //result.reshape(0, new int[] { 192,192});
                    result.convertTo(result, CvType.CV_8U, 255.0);
                    
                    Mat mask192x192 = new Mat(192, 192, CvType.CV_8UC1, (IntPtr)result.dataAddr());

                    Imgproc.resize(mask192x192, maskMat, rgbaMat_2.size(), Imgproc.INTER_NEAREST);
                    
                    // Blur the mask to smooth the edges
                    Imgproc.GaussianBlur(maskMat, maskMat, new Size(5, 5), 0);
                    // Apply a threshold to create a binary mask
                    Imgproc.threshold(maskMat, maskMat, 127, 255, Imgproc.THRESH_BINARY);
                    // Define the structuring element for morphological operations
                    Mat kernel = Imgproc.getStructuringElement(Imgproc.MORPH_RECT, new Size(3, 3));
                    // Apply morphological operations to refine the edges
                    Imgproc.morphologyEx(maskMat, maskMat, Imgproc.MORPH_CLOSE, kernel);
                    // Release the kernel
                    kernel.Dispose();

                    if (composeBGImageToggle.isOn)
                    {
                        // Compose the background image.
                        Core.bitwise_not(maskMat, bgMaskMat);
                        backGroundImageMat.copyTo(rgbaMat_2, bgMaskMat);
                    }

                    if (hidePersonToggle.isOn)
                    {
                        rgbaMat_2.setTo(new Scalar(255, 255, 255, 255), maskMat);
                    }

                    mask192x192.Dispose();
                    result.Dispose();

                    prob.Dispose();
                    blob.Dispose();

                }

                Utils.matToTexture2D(rgbaMat_2, texture);
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