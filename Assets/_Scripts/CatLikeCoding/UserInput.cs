using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


namespace CatLikeCoding
{

	//controls how player input affects other scripts
	public class UserInput : MonoBehaviour
	{
		public LayerMask voxelMapLayerMask;
		bool actionButtonHeld = false;
		VoxelStencil currentStencil;
		public Transform[] stencilVisualisations;
		public Material stencilMaterial;
		public bool snapToGrid = true;
		int radiusIndex = 0;

		private void Awake()
		{
			currentStencil = new VoxelStencil(true, 1);
		}

		private void Update()
		{
			if (Physics.Raycast(Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue()), out RaycastHit hit, Mathf.Infinity, voxelMapLayerMask.value))
			{
				//confirmed hit voxelmap (as long as all objects on voxelMapLayerMask layer are voxel maps, which they should be) 
				VoxelMap map = hit.collider.GetComponent<VoxelMap>();

				Vector2 center = map.transform.InverseTransformPoint(hit.point);
				center.x += map.HalfSize;
				center.y += map.HalfSize;
				if (snapToGrid)
				{
					center.x = ((int)(center.x / map.VoxelSize) + 0.5f) * map.VoxelSize;
					center.y = ((int)(center.y / map.VoxelSize) + 0.5f) * map.VoxelSize;
				}

				//now apply changes to voxelMap
				if (actionButtonHeld)
				{
					currentStencil.Radius = (radiusIndex + 0.5f) * map.VoxelSize;
					map.ApplyStencil(center, currentStencil);
				}

				center.x -= map.HalfSize;
				center.y -= map.HalfSize;
				var visualization = stencilVisualisations[(int)currentStencil.GetStencilType];
				visualization.position = map.transform.TransformPoint(center);
				visualization.localScale = Vector3.one * ((radiusIndex + 0.5f) * map.VoxelSize * 2f);
				visualization.gameObject.SetActive(true);
			}
			else
				stencilVisualisations[(int)currentStencil.GetStencilType].gameObject.SetActive(false);
		}

		void UpdateVisualisation(VoxelMap map, Vector2 hitPoint, out Vector2 center)
		{
			var vis = stencilVisualisations[(int)currentStencil.GetStencilType];
			if (snapToGrid)
			{
				center = (Vector2)map.transform.InverseTransformPoint(hitPoint) + new Vector2(map.HalfSize, map.HalfSize);
				center = new Vector2(((int)(center.x / map.VoxelSize) + 0.5f) * map.VoxelSize - map.HalfSize, ((int)(center.y / map.VoxelSize) + 0.5f) * map.VoxelSize - map.HalfSize);
			}
			else
			{
				center = (Vector2)map.transform.InverseTransformPoint(hitPoint) + new Vector2(map.HalfSize, map.HalfSize);
				center -= Vector2.one * map.HalfSize;
			}
			vis.position = map.transform.TransformPoint(center);
			vis.localScale = Vector3.one * ((currentStencil.Radius + 0.5f) * map.VoxelSize * 2f);
		}

		#region Evaluate Functions

		public bool EvaluateActionHeld()
		{
			return actionButtonHeld;
		}

		#endregion

		//functions called by player input component
		#region Input Functions

		public void OnActionPressed(InputAction.CallbackContext ctx)
		{
			actionButtonHeld = ctx.phase == InputActionPhase.Performed;
		}

		public void OnSwitchFill(InputAction.CallbackContext ctx)
		{
			if (ctx.performed)
			{
				currentStencil.FillType = !currentStencil.FillType;
				Debug.Log("fill: " + currentStencil.FillType);

			}
		}

		public void OnChangeRadius(InputAction.CallbackContext ctx)
		{
			if (ctx.performed)
			{
				if (ctx.ReadValue<float>() > 0)
				{
					radiusIndex = Mathf.Clamp(radiusIndex + 1, 0, 15);
				}
				else
				{
					radiusIndex = Mathf.Clamp(radiusIndex - 1, 0, 15);
				}
				Debug.Log("radius: " + radiusIndex);

			}
		}

		public void OnChangeStencil(InputAction.CallbackContext ctx)
		{
			if (ctx.performed)
			{
				//change stencil visualisation
				stencilVisualisations[(int)currentStencil.GetStencilType].gameObject.SetActive(false);

				//if is a circle, switch to square
				if (currentStencil.GetType() == typeof(VoxelStencilCircle))
				{
					currentStencil = new VoxelStencil(currentStencil.FillType, currentStencil.Radius);
					Debug.Log("Switched to square");
				}
				else
				{
					currentStencil = new VoxelStencilCircle(currentStencil.FillType, currentStencil.Radius);
					Debug.Log("Switched to circle");
				}
			}

		}
		#endregion
	}
}