﻿using UnityEngine;

namespace kTools.Decals
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Projector))]
    public class Decal : MonoBehaviour
    {
        // -------------------------------------------------- //
        //                        ENUM                        //
        // -------------------------------------------------- //

        public enum Axis { PositiveX, NegativeX, PositiveY, NegativeY, PositiveZ, NegativeZ }

        // -------------------------------------------------- //
        //                   PRIVATE FIELDS                   //
        // -------------------------------------------------- //

        private Vector3 m_PreviousScale = Vector3.one;

        private Projector m_Projector;
		public Projector projector
		{
			get 
			{
				if(m_Projector == null)
					m_Projector = GetComponent<Projector>();
				return m_Projector;
			}
		}

        [SerializeField] private DecalData m_DecalData;
        public DecalData decalData
        {
            get { return m_DecalData; }
        }

        private Material m_Material;
        public Material material
        {
            get { return m_Material; }
            set
            {
                if(m_Material)
                    DecalUtil.Destroy(m_Material);
                m_Material = value;
            }
        }

        // -------------------------------------------------- //
        //                ENGINE LOOP METHODS                 //
        // -------------------------------------------------- //

        private void OnEnable()
		{
#if UNITY_EDITOR
            // Collapse Projector UI as user shouldnt edit it
			UnityEditorInternal.InternalEditorUtility.SetIsInspectorExpanded(projector, false);
#endif
            // Initialize the Projector
            SetupProjector();
		}

        private void Update()
        {
            if(transform.localScale != m_PreviousScale)
            {
                m_PreviousScale = transform.localScale;
                SetDecalScale(new Vector2(m_PreviousScale.x, m_PreviousScale.y));
            }
        }

        // -------------------------------------------------- //
        //                   PUBLIC METHODS                   //
        // -------------------------------------------------- //

        /// <summary>
        /// Activates/Deactivates the Decal.
        /// </summary>
        /// <param name="value">Activate or deactivate the Decal.</param>
        public void SetDecalActive(bool value)
        {
            gameObject.SetActive(value);
        }

        /// <summary>
        /// Set DecalData for the Decal and update its renderer.
        /// </summary>
        /// <param name="value">DecalData to set.</param>
        public void SetDecalData(DecalData value)
        {
            m_DecalData = value;
            SetDecalMaterial();
        }

        /// <summary>
        /// Sets a full Decal transform.
        /// </summary>
        /// <param name="positionWS">Decal position in World space.</param>
        /// <param name="rotationWS">Decal rotation in World space.</param>
        /// <param name="scaleWS">Decal scale in World space.</param>
        public void SetDecalTransform(Vector3 positionWS, Quaternion rotationWS, Vector2 scaleWS)
        {
            SetDecalPosition(positionWS);
            SetDecalRotation(rotationWS);
            SetDecalScale(scaleWS);
        }

        /// <summary>
        /// Sets a full Decal transform (using a direction vector for rotation).
        /// </summary>
        /// <param name="positionWS">Decal position in World space.</param>
        /// <param name="directionWS">World space direction/normal vector to use for Decal rotation.</param>
        /// <param name="scaleWS">Decal scale in World space.</param>
        /// <param name="randomRotationZ">If true Decal will be randomly rotated on its local Z axis.</param>
        public void SetDecalTransform(Vector3 positionWS, Vector3 directionWS, Vector2 scaleWS, bool randomRotationZ = false)
        {
            SetDecalPosition(positionWS);
            SetDecalRotation(directionWS, randomRotationZ);
            SetDecalScale(scaleWS);
        }

        // -------------------------------------------------- //
        //                  INTERNAL METHODS                  //
        // -------------------------------------------------- //

        // Initialize Projector state
        private void SetupProjector()
        {
			projector.orthographic = true;
			projector.nearClipPlane = -0.5f;
			projector.farClipPlane = 0.0f;
        }

        // Set Decal position in World space
        private void SetDecalPosition(Vector3 positionWS)
        {
            transform.position = positionWS;
        }

        // Set Decal rotation based on a direction vector
        private void SetDecalRotation(Quaternion rotationWS)
		{
            transform.rotation = rotationWS;
		}

        // Set Decal rotation based on a direction vector
        private void SetDecalRotation(Vector3 directionWS, bool randomRotationZ = false)
		{
            var axis = GetAxisFromDirection(directionWS);
			var randomZ = randomRotationZ ? UnityEngine.Random.Range(0f, 360f) : 0f;
            var rotation = Vector3.zero;
			switch(axis)
			{
				case Axis.NegativeX:
					rotation = new Vector3(0, 90, randomZ);
                    break;
				case Axis.PositiveX:
					rotation = new Vector3(0, -90, randomZ);
                    break;
				case Axis.NegativeY:
					rotation = new Vector3(90, 0, randomZ);
                    break;
				case Axis.PositiveY:
					rotation = new Vector3(-90, 0, randomZ);
                    break;
				case Axis.NegativeZ:
					rotation = new Vector3(0, 0, randomZ);
                    break;
				default: //Axis.PositiveZ
					rotation = new Vector3(0, 180, randomZ);
                    break;
			}
            transform.eulerAngles = rotation;
		}

        // Set Decal scale directly on the Projector
        private void SetDecalScale(Vector2 scaleWS)
        {
			projector.orthographicSize = scaleWS.y * 0.5f;
			projector.aspectRatio = scaleWS.x / scaleWS.y;
        }

        // Get a projection axis from a direction vector
        private Axis GetAxisFromDirection(Vector3 directionWS)
		{
			if(Mathf.Abs(directionWS.x) > 0.5)
				return directionWS.x > 0 ? Axis.NegativeX : Axis.PositiveX;
			else if(Mathf.Abs(directionWS.y) > 0.5)
				return directionWS.y < 0 ? Axis.NegativeY : Axis.PositiveY;
			else //if(Mathf.Abs(direction.z) > 0.5)
				return directionWS.z > 0 ? Axis.NegativeZ : Axis.PositiveZ;
		}

        // Set all Material values on the Decal
        private void SetDecalMaterial()
        {
            // TODO - Replace hack with PropetyBlock after Projector removal
			// TODO - Move to Shader.ToPropertyID

            if(m_DecalData.decalDefinition.shader == null)
            {
                Debug.LogError("No Shader defined for this DecalDefinition. Aborting.");
                return;
            }

            // Initialize material and set common properties
			material = new Material(Shader.Find(m_DecalData.decalDefinition.shader));
			material.SetInt("_Axis", (int)GetAxisFromDirection(transform.forward));

            // Set properties from DecalDefinition
            foreach(DecalProperty prop in decalData.decalDefinition.properties)
                prop.SetProperty(material);

			projector.material = material;
        }
    }
}
