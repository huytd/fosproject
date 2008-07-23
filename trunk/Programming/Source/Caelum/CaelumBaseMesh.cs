/*
This file is part of Caelum for NeoAxis Engine.
Caelum for NeoAxisEngine is a Caelum's modified version.
See http://www.ogre3d.org/wiki/index.php/Caelum for the original version.

Copyright (c) 2008 Heroes -Alliances- team. See Contributors.txt for details.

Caelum for NeoAxis Engine is free software: you can redistribute it and/or modify
it under the terms of the GNU Lesser General Public License as published
by the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

Caelum for NeoAxis Engine is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public License
along with Caelum for NeoAxis Engine. If not, see <http://www.gnu.org/licenses/>.
*/

using Engine.MathEx;
using Engine.Renderer;

namespace Caelum
{
    /// <summary>
    /// Describes a base class for all elements of caelum wich are rendered with a mesh.
    /// </summary>
    public class CaelumBaseMesh : CaelumBase
    {
        // Attributes -----------------------------------------------------------------

        protected MeshObject mMesh;

        // Accessors --------------------------------------------------------------------

        /// <summary>
        /// Gets the main node's material.</summary>
        /// <remarks>Returns null if there isn't material</remarks>
        public Material MainMaterial
        {
            get
            {
                if (mMesh == null )
                    return null;
                else if(mMesh.Mesh == null)
                    return null;
                else if(mMesh.Mesh.SubMeshes.Count == 0)
                    return null;

                string material = mMesh.Mesh.SubMeshes[0].MaterialName;
                return MaterialManager.Instance.GetByName(material);
            }
        }

        // Methods --------------------------------------------------------------------

        ~CaelumBaseMesh()
        {
            Dispose();
        }

        public override void Dispose()
        {
            base.Dispose();

            if (mMesh != null)
                mMesh.Dispose();

            mMesh = null;
        }

        /// <summary>
        /// Creates the element in the world. It automatically
        /// sets up the mesh and the node.</summary>
        protected virtual void Initialise(RenderQueueGroupID renderGroup, string meshName, Vec3 scale, Vec3 rotation)
        {
            // Creates the mesh in the world
            mMesh = SceneManager.Instance.CreateMeshObject(meshName);
            mMesh.CastShadows = false;
            mMesh.RenderQueueGroup = renderGroup;

            // Attaches the mesh on a node
            if (mMesh.ParentSceneNode == null)
            {
                mNode = new SceneNode();
                mNode.Attach(mMesh);
            }
            else
                mNode = mMesh.ParentSceneNode;

            // Sets up the node (Position, Scale and Rotation)
            mNode.Position = Vec3.Zero;
            mNode.Scale = scale;
            mNode.Rotation *= CaelumUtils.GenerateQuat(CaelumUtils.XAxis, new Degree(rotation.X));
            mNode.Rotation *= CaelumUtils.GenerateQuat(CaelumUtils.YAxis, new Degree(rotation.Y));
            mNode.Rotation *= CaelumUtils.GenerateQuat(CaelumUtils.ZAxis, new Degree(rotation.Z));
        }
    }
}
