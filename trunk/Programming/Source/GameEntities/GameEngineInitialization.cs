// Copyright (C) 2006-2008 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Engine;
using Engine.Renderer;
using Engine.MapSystem;
using GameCommon;

namespace GameEntities
{
	/// <summary>
	/// Class for execute actions after initialization of the engine.
	/// </summary>
	/// <remarks>
	/// It is class works in simulation application and editors (Resource Editor, Map Editor).
	/// </remarks>
	public class GameEngineInitialization : EngineInitialization
	{
		protected override bool OnInit()
		{
			CorrectRenderTechnique();

			//Initialize HDR compositor for HDR render technique
			if( EngineApp.RenderTechnique == "HDR" )
				InitializeHDRCompositor();

			return true;
		}

		bool IsHDRSupported()
		{
			Compositor compositor = CompositorManager.Instance.GetByName( "HDR" );
			if( compositor == null || !compositor.IsSupported() )
				return false;

			bool floatTexturesSupported = TextureManager.Instance.IsEquivalentFormatSupported(
				Texture.Type.Type2D, PixelFormat.Float16RGB, Texture.Usage.RenderTarget );
			if( !floatTexturesSupported )
				return false;

			//!!!!!need also check for support blending for float textures

			if( RenderSystem.Instance.GPUIsGeForce() &&
				RenderSystem.Instance.GPUCodeName >= GPUCodeNames.GeForce_NV10 &&
				RenderSystem.Instance.GPUCodeName <= GPUCodeNames.GeForce_NV30 )
				return false;
			if( RenderSystem.Instance.GPUIsRadeon() &&
				RenderSystem.Instance.GPUCodeName >= GPUCodeNames.Radeon_R100 &&
				RenderSystem.Instance.GPUCodeName <= GPUCodeNames.Radeon_R400 )
				return false;

			return true;
		}

		bool IsActivateHDRByDefault()
		{
			//!!!!!!temporary disabled.
			//because of a bug at change window sizes in the Direct3D

			//if( IsHDRSupported() )
			//{
			//   //NV40 support hdr, but we not activate on it gpu by default.
			//   if( RenderSystem.Instance.GPUIsGeForce() &&
			//      RenderSystem.Instance.GPUCodeName >= GPUCodeNames.GeForce_G70 )
			//      return true;
			//   if( RenderSystem.Instance.GPUIsRadeon() &&
			//      RenderSystem.Instance.GPUCodeName >= GPUCodeNames.Radeon_R500 )
			//      return true;
			//}

			return false;
		}

		void CorrectRenderTechnique()
		{
			//HDR choose by default
			if( string.IsNullOrEmpty( EngineApp.RenderTechnique ) && IsHDRSupported() )
				EngineApp.RenderTechnique = IsActivateHDRByDefault() ? "HDR" : "Standard";

			//HDR render technique support check
			if( EngineApp.RenderTechnique == "HDR" && !IsHDRSupported() )
			{
				Log.Warning( "HDR render technique is not supported. " +
					"Using \"Standard\" render technique." );
				EngineApp.RenderTechnique = "Standard";
			}

			if( string.IsNullOrEmpty( EngineApp.RenderTechnique ) )
				EngineApp.RenderTechnique = "Standard";
		}

		void InitializeHDRCompositor()
		{
			bool editor = EngineApp.Instance.IsResourceEditor || EngineApp.Instance.IsMapEditor;

			//Add HDR compositor
			HDRCompositorInstance instance = (HDRCompositorInstance)
				RendererWorld.Instance.DefaultViewport.AddCompositor( "HDR", 0 );

			if( instance == null )
				return;

			//Enable HDR compositor
			instance.Enabled = true;

			//Load default settings
			TextBlock fileBlock = TextBlockUtils.LoadFromVirtualFile( "Definitions/Renderer.config" );
			if( fileBlock != null )
			{
				TextBlock hdrBlock = fileBlock.FindChild( "hdr" );
				if( hdrBlock != null )
				{
					if( !editor )//No adaptation in the editors
					{
						if( hdrBlock.IsAttributeExist( "adaptation" ) )
							instance.Adaptation = bool.Parse( hdrBlock.GetAttribute( "adaptation" ) );

						if( hdrBlock.IsAttributeExist( "adaptationVelocity" ) )
							instance.AdaptationVelocity =
								float.Parse( hdrBlock.GetAttribute( "adaptationVelocity" ) );

						if( hdrBlock.IsAttributeExist( "adaptationMiddleBrightness" ) )
							instance.AdaptationMiddleBrightness =
								float.Parse( hdrBlock.GetAttribute( "adaptationMiddleBrightness" ) );

						if( hdrBlock.IsAttributeExist( "adaptationMinimum" ) )
							instance.AdaptationMinimum =
								float.Parse( hdrBlock.GetAttribute( "adaptationMinimum" ) );

						if( hdrBlock.IsAttributeExist( "adaptationMaximum" ) )
							instance.AdaptationMaximum =
								float.Parse( hdrBlock.GetAttribute( "adaptationMaximum" ) );
					}

					if( hdrBlock.IsAttributeExist( "bloomBrightThreshold" ) )
						instance.BloomBrightThreshold =
							float.Parse( hdrBlock.GetAttribute( "bloomBrightThreshold" ) );

					if( hdrBlock.IsAttributeExist( "bloomScale" ) )
						instance.BloomScale = float.Parse( hdrBlock.GetAttribute( "bloomScale" ) );
				}
			}
		}
	}
}
