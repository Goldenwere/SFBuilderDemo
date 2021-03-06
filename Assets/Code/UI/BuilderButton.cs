﻿using UnityEngine;
using UnityEngine.UI;
using SFBuilder.Obj;
using Goldenwere.Unity.UI;
using System.Collections.Generic;

namespace SFBuilder.UI
{
    /// <summary>
    /// An instance of BuilderButton is attached to a Button prefab to give it functionality related to object placement
    /// </summary>
    public class BuilderButton : MonoBehaviour
    {
        #region Fields
#pragma warning disable 0649
        [SerializeField] private Button                 button;
        [SerializeField] private Image                  buttonImage;
        [SerializeField] private TMPro.TMP_Text         indicatorCount;
        [SerializeField] private Image                  indicatorIcon;
        [SerializeField] private TMPro.TMP_Text         indicatorID;
        [SerializeField] private TooltipEnabledElement  tooltipElement;
#pragma warning restore 0649
        /**************/ private int                    associatedCount;
        /**************/ private int                    associatedID;
        /**************/ private bool                   initialized;
        /**************/ private bool                   isRequired;
        /**************/ private List<BuilderObject>    spawnedObjects;
        #endregion
        #region Methods
        /// <summary>
        /// Subscribe to events On Enable
        /// </summary>
        private void OnEnable()
        {
            if (spawnedObjects != null)
            {
                foreach (BuilderObject spawned in spawnedObjects)
                {
                    spawned.objectPlaced += OnObjectPlaced;
                    spawned.objectRecalled += OnObjectRecalled;
                }
            }
        }

        /// <summary>
        /// Unsubscribe from events On Disable
        /// </summary>
        private void OnDisable()
        {
            if (spawnedObjects != null)
            {
                foreach (BuilderObject spawned in spawnedObjects)
                {
                    spawned.objectPlaced -= OnObjectPlaced;
                    spawned.objectRecalled -= OnObjectRecalled;
                }
            }
        }

        /// <summary>
        /// When the button is first being created, call this to associate it with a BuilderObject
        /// </summary>
        /// <param name="info">The info to set the button up with</param>
        public void SetupButton(ButtonInfo info)
        {
            if (!initialized)
            {
                associatedCount = info.count;
                associatedID = info.id;
                isRequired = info.req;
                Sprite s = GameUI.Instance.GetIcon((ObjectType)info.id);
                if (s != null)
                {
                    indicatorIcon.sprite = s;
                    indicatorID.gameObject.SetActive(false);
                }
                else
                {
                    indicatorID.text = BuilderObject.NameOfType((ObjectType)info.id);
                    indicatorIcon.gameObject.SetActive(false);
                }
                indicatorCount.text = info.count.ToString();
                button.interactable = associatedCount > 0;
                BuilderObject.DescriptionOfType((ObjectType)info.id, out string desc);
                tooltipElement.UpdateText(desc);
                spawnedObjects = new List<BuilderObject>(info.count);
                initialized = true;
            }
        }

        /// <summary>
        /// When the button is first being created, call this to associate it with a BuilderObject
        /// </summary>
        /// <param name="info">The info to set the button up with</param>
        /// <param name="palette">Color palette to load into the button</param>
        public void SetupButton(ButtonInfo info, ColorPalette palette)
        {
            SetupButton(info);
            if (isRequired)
                buttonImage.color = palette.PlacementRequiredColor;
            else
                buttonImage.color = palette.PlacementExtraColor;
        }

        /// <summary>
        /// Handler for button's press event, which utilized the PlacementSystem to spawn a BuilderObject
        /// </summary>
        public void OnButtonPress()
        {
            if (associatedCount > 0)
            {
                BuilderObject spawned = PlacementSystem.Instance.OnObjectSelected(associatedID);
                if (spawned != null)
                {
                    spawned.objectPlaced += OnObjectPlaced;
                    spawned.objectRecalled += OnObjectRecalled;
                    spawnedObjects.Add(spawned);
                }
            }
        }

        /// <summary>
        /// Handler for a BuilderObject's objectPlaced event which decrements the associated count on the button and the current goal working set
        /// </summary>
        /// <param name="obj">The object that was placed</param>
        private void OnObjectPlaced(BuilderObject obj)
        {
            associatedCount--;
            indicatorCount.text = associatedCount.ToString();
            if (associatedCount <= 0)
                button.interactable = false;
            GameEventSystem.Instance.UpdateGoalAmount(false, associatedID, isRequired);
        }

        /// <summary>
        /// Handler for a BuilderObject's objectRecalled event (called by Undo) which increments the associated count on the button and the current goal working set
        /// </summary>
        /// <param name="obj">The object that was recalled</param>
        private void OnObjectRecalled(BuilderObject obj)
        {
            associatedCount++;
            indicatorCount.text = associatedCount.ToString();
            if (associatedCount > 0 && !button.interactable)
                button.interactable = true;
            GameEventSystem.Instance.UpdateGoalAmount(true, associatedID, isRequired);
        }
        #endregion
    }
}