﻿/*
 * Copyright (c) <2020> Side Effects Software Inc.
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *
 * 1. Redistributions of source code must retain the above copyright notice,
 *    this list of conditions and the following disclaimer.
 *
 * 2. The name of Side Effects Software may not be used to endorse or
 *    promote products derived from this software without specific prior
 *    written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY SIDE EFFECTS SOFTWARE "AS IS" AND ANY EXPRESS
 * OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.  IN
 * NO EVENT SHALL SIDE EFFECTS SOFTWARE BE LIABLE FOR ANY DIRECT, INDIRECT,
 * INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
 * LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA,
 * OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
 * LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
 * NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE,
 * EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using UnityEngine;
using System.Collections.Generic;

// Expose internal classes/functions
#if UNITY_EDITOR
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("HoudiniEngineUnityEditor")]
[assembly: InternalsVisibleTo("HoudiniEngineUnityEditorTests")]
[assembly: InternalsVisibleTo("HoudiniEngineUnityPlayModeTests")]
#endif

namespace HoudiniEngineUnity
{
    /// <summary>
    /// The root object of a Houdini Engine asset.
    /// Used for organizing hierarchy, and more importantly displaying custom UI.
    /// </summary>
    [SelectionBase]
    [ExecuteInEditMode] // Needed to get OnDestroy callback when deleted in Editor
    public class HEU_HoudiniAssetRoot : MonoBehaviour
    {
        // Reference to the actual Houdini Engine asset gamebobject which contains
        // all the data and logic to work with Houdini Engine
        [SerializeField] internal HEU_HoudiniAsset _houdiniAsset;
        public HEU_HoudiniAsset HoudiniAsset => _houdiniAsset;

        [SerializeField] internal List<GameObject> _bakeTargets = new List<GameObject>();
        public List<GameObject> BakeTargets => _bakeTargets;

        /// <summary>
        /// Callback when asset is deleted. Removes assset from Houdini session if in Editor.
        /// </summary>
        private void OnDestroy()
        {
            if (_houdiniAsset != null && _houdiniAsset.PauseCooking == true)
            {
                return;
            }

            // Destroy the asset from session or permanently. 
            // The following checks make sure to only delete if the scene is closing, 
            // or asset has been user deleted. 
            // Skips if its just transitioning into or out of play mode.
            // TODO RUNTIME: if/when we support runtime, should only do the Application.isEditor check if in Editor			
            if (_houdiniAsset != null && HEU_EditorUtility.IsEditorNotInPlayModeAndNotGoingToPlayMode())
            {
                // Delete mesh data if this asset hasn't been saved and it is a user invoked delete event.
                // Otherwise just remove from session.
                // TODO: for saved assets, we need to handle case where user deletes but does not save scene after
                if (!_houdiniAsset.IsAssetSavedInScene() && (Event.current != null &&
                                                             (Event.current.commandName.Equals("Delete") ||
                                                              Event.current.commandName.Equals("SoftDelete"))))
                {
                    _houdiniAsset.DeleteAssetCacheData(bRegisterUndo: true);
                }

                if (Event.current != null && Event.current.commandName.Equals("SoftDelete"))
                {
                    // Don't delete data so that you can undo!
                    _houdiniAsset.SetSoftDeleted();
                }
                else
                {
                    _houdiniAsset.DeleteAllGeneratedData(bIsRebuild: false);
                }
            }
        }

        /// <summary>
        /// Removes all Houdini Engine data from this asset.
        /// Leaves this gameobject and its children including Unity-specific
        /// components like geometry, materials, etc.
        /// </summary>
        internal void RemoveHoudiniEngineAssetData()
        {
            HEU_EditorUtility.UndoRecordObject(this, "Clear References");
            // TODO: try Undo.RegisterCompleteObjectUndo or  RegisterFullObjectHierarchyUndo

            if (_houdiniAsset != null)
            {
                // We'll do a simple DestroyImmediate.
                // No need to destroy the object, geo nodes, and parts
                // since Unity's GC will handle them.

                GameObject tempGO = _houdiniAsset.gameObject;
                _houdiniAsset = null;
                HEU_GeneralUtility.DestroyImmediate(tempGO, bRegisterUndo: true);
            }

            ClearHoudiniEngineReferences();
            DestroyRootComponent(this);

            HEU_EditorUtility.UndoCollapseCurrentGroup();
        }

        internal void ClearHoudiniEngineReferences()
        {
            _houdiniAsset = null;
            _bakeTargets.Clear();
        }

        internal static void DestroyRootComponent(HEU_HoudiniAssetRoot assetRoot)
        {
            HEU_GeneralUtility.DestroyImmediate(assetRoot, bRegisterUndo: true);
        }

        internal void Reset()
        {
            // Unity calls this to reset this component.
            // _houdiniAsset will be null since that is the default value.
            // So just reconnect _houdiniAsset, then reset all HDA parms and rebuild it.

            if (_houdiniAsset == null)
            {
                _houdiniAsset = transform.GetComponentInChildren<HEU_HoudiniAsset>();
            }

            if (_houdiniAsset != null)
            {
                _houdiniAsset.RequestResetParameters(false);
            }
        }
    }
} // HoudiniEngineUnity