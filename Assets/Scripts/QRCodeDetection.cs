using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using ZXing;
using ZXing.Common;
using TMPro;
using System.Collections;
using Unity.Collections;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(ARCameraManager))]
public class ARFoundationQRCodeDetection : MonoBehaviour
{
    // Variable Creation
    private ARCameraManager arCameraManager;
    private BarcodeReader barcodeReader;

    public GameObject cubePrefab;
    public ARRaycastManager raycastManager;
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private bool SpawnedCube = false;
    public MenuController menuController;
    public TextMeshProUGUI debugText;

    void Awake()
    {
        //Gets the camera manager
        arCameraManager = GetComponent<ARCameraManager>();

        //Creates the QR code reader & sets the options
        barcodeReader = new BarcodeReader
        {
            AutoRotate = true,
            Options = new DecodingOptions
            {
                TryHarder = true,
                PossibleFormats = new System.Collections.Generic.List<BarcodeFormat> { BarcodeFormat.QR_CODE }
            }
        };

        //Catchment if it can't find the camera manager to grab it manually
        arCameraManager = GetComponent<ARCameraManager>();
        if (raycastManager == null)
            raycastManager = Object.FindFirstObjectByType<ARRaycastManager>();

    }

    // Used to get the camera frames 
    void OnEnable()
    {
        arCameraManager.frameReceived += OnCameraFrameReceived;
    }

    void OnDisable()
    {
        arCameraManager.frameReceived -= OnCameraFrameReceived;
    }

    private void OnCameraFrameReceived(ARCameraFrameEventArgs eventArgs)
    {
        // Tries to get the raw CPU-readable image from the AR camera
        if (!arCameraManager.TryAcquireLatestCpuImage(out var cpuImage))
        {
            return;
        }
        // Passes the image to process it
        StartCoroutine(ProcessImage(cpuImage));
    }

    private IEnumerator ProcessImage(UnityEngine.XR.ARSubsystems.XRCpuImage cpuImage)
    {
        //Defines how to convert the image 
        var conversionParams = new UnityEngine.XR.ARSubsystems.XRCpuImage.ConversionParams
        {
            inputRect = new RectInt(0, 0, cpuImage.width, cpuImage.height),
            outputDimensions = new Vector2Int(cpuImage.width, cpuImage.height),
            outputFormat = TextureFormat.R8,
            transformation = UnityEngine.XR.ARSubsystems.XRCpuImage.Transformation.MirrorY
        };

        //Allocate buffer and convert image to grayscale byte data
        int size = cpuImage.GetConvertedDataSize(conversionParams);
        var buffer = new NativeArray<byte>(size, Allocator.Temp);

        cpuImage.Convert(conversionParams, buffer);
        cpuImage.Dispose();

        // Converts the greyscale to colour - needed by ZXing
        var pixels = new Color32[size];
        for (int i = 0; i < size; i++)
        {
            byte gray = buffer[i];
            pixels[i] = new Color32(gray, gray, gray, 255);
        }
        buffer.Dispose();

        // Decoding params
        int width = conversionParams.outputDimensions.x;
        int height = conversionParams.outputDimensions.y;

        // Try normal decode
        var result = barcodeReader.Decode(pixels, width, height);

        // Try rotated decode if normal fails
        if (result == null)
        {
            var rotatedPixels = RotateImage90Clockwise(pixels, width, height);
            result = barcodeReader.Decode(rotatedPixels, height, width);
        }

        // Try inverted decode if still no result
        if (result == null)
        {
            var invertedPixels = InvertColors(pixels);
            result = barcodeReader.Decode(invertedPixels, width, height);
        }

        if (result != null && !SpawnedCube)
        {

            // Estimate center of QR code in image space
            float avgX = result.ResultPoints.Average(p => p.X);
            float avgY = result.ResultPoints.Average(p => p.Y);


            // Convert camera image pixel to screen space (scaled)
            float screenX = (avgX / width) * Screen.width;
            float screenY = Screen.height - ((avgY / height) * Screen.height);
            screenX = Mathf.Clamp(screenX, 0, Screen.width);
            screenY = Mathf.Clamp(screenY, 0, Screen.height);

            Vector2 screenPoint = new Vector2(screenX, screenY);


            // Raycast into AR world
            if (raycastManager.Raycast(screenPoint, hits, TrackableType.AllTypes))
            {
                // Spawns the cube at the position found by the raycast
                var cubeInstance = Instantiate(cubePrefab, hits[0].pose.position, hits[0].pose.rotation);
                cubeInstance.transform.localScale = Vector3.one * 0.1f;
                var cubeController = cubeInstance.GetComponent<CubeController>();
                MenuController.Instance.SetCurrentCube(cubeController); 
                SpawnedCube = true;
            }
        }

        yield return null;
    }

    private Color32[] RotateImage90Clockwise(Color32[] src, int width, int height)
    {
        Color32[] dest = new Color32[src.Length];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                dest[x * height + (height - y - 1)] = src[y * width + x];
        return dest;
    }

    private Color32[] InvertColors(Color32[] src)
    {
        Color32[] inverted = new Color32[src.Length];
        for (int i = 0; i < src.Length; i++)
        {
            byte inv = (byte)(255 - src[i].r);
            inverted[i] = new Color32(inv, inv, inv, 255);
        }
        return inverted;
    }
}
