// Copyright (C) 2006-2008 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Engine;
using Engine.UISystem;

namespace Game
{
	/// <summary>
	/// Defines a "Debug Draw Options" window.
	/// </summary>
	public class DebugDrawOptionsWindow : EControl
	{
		EControl window;

		protected override void OnAttach()
		{
			base.OnAttach();

			window = ControlDeclarationManager.Instance.CreateControl( "Gui\\DebugDrawOptionsWindow.gui" );
			Controls.Add( window );

			InitCheckBox( "StaticPhysics", typeof( GameWindow ).GetField( "mapDrawStaticPhysics" ) );
			InitCheckBox( "DynamicPhysics", typeof( GameWindow ).GetField( "mapDrawDynamicPhysics" ) );
			InitCheckBox( "SceneGraphInfo", typeof( GameWindow ).GetField( "mapDrawSceneGraphInfo" ) );
			InitCheckBox( "Regions", typeof( GameWindow ).GetField( "mapDrawRegions" ) );
			InitCheckBox( "MapObjectBounds", typeof( GameWindow ).GetField( "mapDrawMapObjectBounds" ) );
			InitCheckBox( "SceneNodeBounds", typeof( GameWindow ).GetField( "mapDrawSceneNodeBounds" ) );
			InitCheckBox( "StaticMeshObjectBounds", typeof( GameWindow ).GetField( "mapDrawStaticMeshObjectBounds" ) );
			InitCheckBox( "ZonesPortalsOccluders", typeof( GameWindow ).GetField( "mapDrawZonesPortalsOccluders" ) );
			InitCheckBox( "FrustumTest", typeof( GameWindow ).GetField( "frustumTest" ) );
			InitCheckBox( "Lights", typeof( GameWindow ).GetField( "mapDrawLights" ) );
			InitCheckBox( "StaticGeometry", typeof( GameWindow ).GetField( "mapDrawStaticGeometry" ) );
			InitCheckBox( "Models", typeof( GameWindow ).GetField( "mapDrawModels" ) );
			InitCheckBox( "Effects", typeof( GameWindow ).GetField( "mapDrawEffects" ) );
			InitCheckBox( "Gui", typeof( GameWindow ).GetField( "mapDrawGui" ) );
			InitCheckBox( "Wireframe", typeof( GameWindow ).GetField( "mapDrawWireframe" ) );
			InitCheckBox( "PostEffects", typeof( GameWindow ).GetField( "drawPostEffects" ) );
			InitCheckBox( "PerformanceCounter", typeof( GameWindow ).GetField( "drawPerformanceCounter" ) );
			InitCheckBox( "GameSpecificDebugGeometry", typeof( GameWindow ).GetField( "drawGameSpecificDebugGeometry" ) );

			( (EButton)window.Controls[ "Defaults" ] ).Click += Defaults_Click;

			( (EButton)window.Controls[ "Close" ] ).Click += delegate( EButton sender )
			{
				SetShouldDetach();
			};
		}

		void Defaults_Click( EButton sender )
		{
			foreach( EControl control in window.Controls )
			{
				ECheckBox checkBox = control as ECheckBox;
				if( checkBox == null )
					continue;

				FieldInfo field = checkBox.UserData as FieldInfo;
				if( field == null )
					continue;

				ConfigAttribute[] attributes = (ConfigAttribute[])field.GetCustomAttributes(
					typeof( ConfigAttribute ), true );

				if( attributes.Length == 0 )
					continue;

				Config.Parameter parameter = EngineApp.Instance.Config.GetParameter(
					attributes[ 0 ].GroupPath, attributes[ 0 ].Name );

				checkBox.Checked = bool.Parse( parameter.DefaultValue );
			}
		}

		void InitCheckBox( string name, FieldInfo field )
		{
			ECheckBox checkBox = (ECheckBox)window.Controls[ name ];
			checkBox.UserData = field;

			checkBox.Checked = (bool)field.GetValue( null );

			checkBox.CheckedChange += delegate( ECheckBox sender )
			{
				field.SetValue( null, !(bool)field.GetValue( null ) );
			};
		}

		protected override bool OnKeyDown( KeyEvent e )
		{
			if( base.OnKeyDown( e ) )
				return true;
			if( e.Key == EKeys.Escape )
			{
				SetShouldDetach();
				return true;
			}
			return false;
		}

	}
}
