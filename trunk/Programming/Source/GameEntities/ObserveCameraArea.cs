// Copyright (C) 2006-2008 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using Engine.Utils;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.Renderer;
using Engine.PhysicsSystem;
using Engine.MathEx;

namespace GameEntities
{
	/// <summary>
	/// Defines the <see cref="ObserveCameraArea"/> entity type.
	/// </summary>
	public class ObserveCameraAreaType : MapObjectType
	{
	}

	public class ObserveCameraArea : MapObject
	{
		[FieldSerialize]
		MapCamera mapCamera;

		[FieldSerialize]
		MapCurve mapCurve;

		ObserveCameraAreaType _type = null; public new ObserveCameraAreaType Type { get { return _type; } }

		public MapCamera MapCamera
		{
			get { return mapCamera; }
			set
			{
				if( mapCamera != null )
					RemoveRelationship( mapCamera );
				mapCamera = value;
				if( mapCamera != null )
					AddRelationship( mapCamera );
			}
		}

		public MapCurve MapCurve
		{
			get { return mapCurve; }
			set
			{
				if( mapCurve != null )
					RemoveRelationship( mapCurve );
				mapCurve = value;
				if( mapCurve != null )
					AddRelationship( mapCurve );
			}
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnRelatedEntityDelete(Entity)"/></summary>
		protected override void OnRelatedEntityDelete( Entity entity )
		{
			base.OnRelatedEntityDelete( entity );

			if( entity == mapCamera )
				mapCamera = null;
			if( entity == mapCurve )
				mapCurve = null;
		}

		/// <summary>Overridden from <see cref="Engine.MapSystem.MapObject.OnRender(Camera)"/>.</summary>
		protected override void OnRender( Camera camera )
		{
			base.OnRender( camera );

			//bool render;

			//if( EntitySystemWorld.Instance.WorldSimulationType == WorldSimulationType.Editor )
			//   render = true;
			//else
			//   render = Map.Instance.DrawRegions && camera == RendererWorld.Instance.DefaultCamera;

			//if( render )
			if( EntitySystemWorld.Instance.WorldSimulationType == WorldSimulationType.Editor &&
				camera == RendererWorld.Instance.DefaultCamera && EditorLayer.Visible )
			{
				DebugGeometry.Instance.Color = new ColorValue( 0, 0, 1 );
				DebugGeometry.Instance.AddBox( GetBox() );
			}

			/*
			if( PhysicsModel != null )
			{
				//PhysicsWorld.Instance.SetForceDebugRenderColor( new ColorValue( 0, 0, 1 ) );
				foreach( Body body in PhysicsModel.Bodies )
					body.DebugRender( 0, 1, true );
				//PhysicsWorld.Instance.ResetForceDebugRenderColor();
			}
			*/

		}

	}
}
