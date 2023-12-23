using UnityEngine;
using UnityEngine.Assertions;

namespace Game
{
	/// <summary>
	/// Camera controller for Game and Final scenes. Fits the Scene area delimited by markers onto the UI frame.
	/// </summary>
	[DisallowMultipleComponent, RequireComponent(typeof(Camera)), ExecuteInEditMode]
	public sealed class GameCameraController : MonoBehaviour
	{
		// The delimiters
		[SerializeField, Header("Scene")] private Transform _leftBottomPoint;
		[SerializeField] private Transform _rightTopPoint;
		// The frame into which the scene fits
		[SerializeField, Header("UI")] private RectTransform _uiFrame;

#if UNITY_EDITOR
		private int? _screenWidth, _screenHeight;
#endif

		private void Start()
		{
#if UNITY_EDITOR
			_screenWidth = Screen.width;
			_screenHeight = Screen.height;
#endif
			AdjustCamera();
		}

#if UNITY_EDITOR
		private void Update()
		{
			if (_screenWidth != Screen.width || _screenHeight != Screen.height)
			{
				_screenWidth = Screen.width;
				_screenHeight = Screen.height;

				AdjustCamera();
			}
		}
#endif

		private void AdjustCamera()
		{
			Assert.IsTrue(_leftBottomPoint && _rightTopPoint && _uiFrame,
				"Screen size markers and UI frame must have.");

			Vector3 newCameraPosition;
			var canvas = _uiFrame.GetComponentInParent<Canvas>();
			if (!canvas)
			{
				Debug.LogError("Can't find root Canvas.");
				return;
			}

			var lbp = _leftBottomPoint.position;
			var rtp = _rightTopPoint.position;
			var sizeX = rtp.x - lbp.x;
			var sizeY = rtp.y - lbp.y;
			var sizeZ = Mathf.Max(rtp.z - lbp.z, 0);
			if (sizeX <= 0 || sizeY <= 0)
			{
				Debug.LogError("Scene size points has wrong position (delta <= 0).");
				return;
			}

			var canvasScaleFactor = canvas.scaleFactor;
			var frameBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(_uiFrame);
			var framePosition = (Vector2)_uiFrame.position;
			var frameMin = framePosition / canvasScaleFactor + (Vector2)frameBounds.min;
			var canvasSize = canvas.pixelRect.size;
			var fieldBounds = new Bounds(new Vector3(lbp.x + sizeX * 0.5f, lbp.y + sizeY * 0.5f, lbp.z + sizeZ * 0.5f),
				new Vector3(sizeX, sizeY, sizeZ));
			var fieldSize = fieldBounds.size;
			var frameSize = frameBounds.size;

			var k = frameSize.x / frameSize.y > fieldSize.x / fieldSize.y
				? fieldSize.y / frameSize.y
				: fieldSize.x / frameSize.x;

			fieldBounds.size = new Vector3(frameSize.x * k, frameSize.y * k, fieldSize.z);
			var screenMin = (Vector3)((Vector2)fieldBounds.min - frameMin * k) +
			                Vector3.forward * fieldBounds.min.z;
			var screenMax = screenMin + (Vector3)(canvasSize * k / canvasScaleFactor);
			var screenBounds = new Bounds { min = screenMin, max = screenMax };

			var cam = GetComponent<Camera>();
			Assert.IsTrue(cam, "Camera must have.");
			if (cam.orthographic)
			{
				newCameraPosition = new Vector3(screenBounds.center.x, screenBounds.center.y, transform.position.z);
				cam.orthographicSize = screenBounds.size.y * 0.5f;
			}
			else
			{
				var ang = cam.fieldOfView * 0.5f;
				var zDistance = screenBounds.size.y * 0.5f / Mathf.Tan(ang * Mathf.Deg2Rad);
				newCameraPosition = screenBounds.center + Vector3.back * zDistance;
			}

			transform.position = newCameraPosition;
		}
	}
}