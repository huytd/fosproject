// Copyright (C) 2006-2008 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Engine;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.Renderer;

namespace WindowsAppExample
{
	public partial class MainForm : Form
	{
		static MainForm instance;

		float lastRenderTime;

		//

		public MainForm()
		{
			instance = this;
			InitializeComponent();

			labelEngineVersion.Text += " " + EngineVersionInformation.Version;
		}

		public static MainForm Instance
		{
			get { return instance; }
		}

		private void MainForm_Load( object sender, EventArgs e )
		{
			EngineApp.Instance.ParentWindowControl = panelEngine;
			if( !EngineApp.Instance.Create() )
			{
				Close();
				return;
			}

			timer1.Start();

			trackBarSoundVolume.Value = (int)( WindowsAppEngineApp.SoundVolume * 1000 );

			//load map
			MapLoad( "Maps\\WindowsAppExample\\Map.map" );
		}

		private void MainForm_FormClosing( object sender, FormClosingEventArgs e )
		{
			EngineApp.Instance.Destroy();

			instance = null;
		}

		private void buttonExit_Click( object sender, EventArgs e )
		{
			Close();
		}
				
		internal bool IsAllowRenderScene()
		{
			Form activeForm = ActiveForm;
			if( activeForm == null )
				return false;

			Form form = activeForm;
			while( form != null )
			{
				if( form == this )
					return true;
				if( form.Modal )
					return false;
				form = form.Owner;
			}

			return false;
		}

		private void timer1_Tick( object sender, EventArgs e )
		{
			//render via timer only for modal dialog in main form
			if( Visible && WindowState != FormWindowState.Minimized && IsAllowRenderScene() )
				if( ActiveForm != this )
					RenderScene();

			if( !Visible || WindowState == FormWindowState.Minimized )
				UnloadAllLoadableResources();

			if( !Visible || WindowState == FormWindowState.Minimized || !IsAllowRenderScene() )
				EngineApp.Instance.KeysAndMouseButtonUpAll();
		}

		private void panelEngine_Paint( object sender, PaintEventArgs e )
		{
			if( Visible && WindowState != FormWindowState.Minimized && !IsAllowRenderScene() )
				RenderScene();
		}

		internal void RenderScene()
		{
			if( RenderSystem.Instance.IsDeviceLostByTestCooperativeLevel() )
				return;
			if( !Visible || WindowState == FormWindowState.Minimized )
				return;

			//max fps
			const float maxFPS = 50.0f;
			float renderTime = EngineApp.Instance.Time;
			if( renderTime < lastRenderTime + 1.0f / maxFPS )
				return;
			lastRenderTime = renderTime;

			//render
			if( EngineApp.Instance.WindowControl != null )
			{
				Rectangle clientRect = panelEngine.ClientRectangle;

				if( EngineApp.Instance.WindowControl.Location != clientRect.Location ||
					EngineApp.Instance.WindowControl.Size != clientRect.Size )
				{
					EngineApp.Instance.WindowControl.Location = clientRect.Location;
					EngineApp.Instance.WindowControl.Size = clientRect.Size;
					EngineApp.Instance.OnResize();
				}

				EngineApp.Instance.DoTick();
				EngineApp.Instance.RenderScene();
			}
		}

		WorldType GetWorldType()
		{
			List<EntityType> list = EntityTypes.Instance.GetTypesBasedOnClass(
				EntityTypes.Instance.GetClassInfoByEntityClassName( "World" ) );
			if( list.Count == 0 )
			{
				Log.Error( "World type not defined." );
				return null;
			}
			if( list.Count != 1 )
			{
				Log.Error( "World type need alone." );
				return null;
			}
			return (WorldType)list[ 0 ];
		}

		bool MapLoad( string fileName )
		{
			//Destroy old
			MapDestroy();

			//New
			WorldType worldType = GetWorldType();
			if( worldType == null )
				return false;
			if( !EntitySystemWorld.Instance.WorldCreate( WorldSimulationType.Single, worldType ) )
			{
				Log.Error( "EntitySystemWorld: WorldCreate failed." );
				return false;
			}

			if( !MapSystemWorld.MapLoad( fileName, false ) )
				return false;

			//run simulation
			EntitySystemWorld.Instance.Simulation = true;

			return true;
		}

		void MapDestroy()
		{
			MapSystemWorld.MapDestroy();
		}

		void UnloadAllLoadableResources()
		{
			TextureManager.Instance.UnloadAll( true );
		}

		private void trackBarSoundVolume_Scroll( object sender, EventArgs e )
		{
			WindowsAppEngineApp.SoundVolume = (float)( trackBarSoundVolume.Value + .5f ) / 1000.0f;
		}
	}
}