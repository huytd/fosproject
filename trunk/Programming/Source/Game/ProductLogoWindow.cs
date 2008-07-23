// Copyright (C) 2006-2008 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using Engine;
using Engine.UISystem;
using Engine.Renderer;
using Engine.MathEx;
using Engine.SoundSystem;

namespace Game
{
	/// <summary>
	/// Defines a your project logo window.
	/// </summary>
	public class ProductLogoWindow : EControl
	{
		const float lifeTime = 10;

		Texture productTexture;

		protected override void OnAttach()
		{
			base.OnAttach();

			productTexture = TextureManager.Instance.Load( "Utils\\Product" );

			EngineApp.Instance.MouseRelativeMode = true;

			SoundWorld.Instance.SetListener( new Vec3( 1000, 1000, 1000 ),
				Vec3.Zero, new Vec3( 1, 0, 0 ), new Vec3( 0, 0, 1 ) );

			ResetTime();
		}

		protected override void OnDetach()
		{
			EngineApp.Instance.MouseRelativeMode = false;
			EngineApp.Instance.MousePosition = new Vec2( .9f, .8f );

			base.OnDetach();
		}

		protected override bool OnKeyDown( KeyEvent e )
		{
			if( base.OnKeyDown( e ) )
				return true;
			if( e.Key == EKeys.Escape || e.Key == EKeys.Return || e.Key == EKeys.Space )
			{
				Destroy();
				return true;
			}
			return false;
		}

		protected override bool OnMouseDown( EMouseButtons button )
		{
			if( button == EMouseButtons.Left || button == EMouseButtons.Right )
			{
				Destroy();
				return true;
			}
			return base.OnMouseDown( button );
		}

		protected override void OnTick( float delta )
		{
			base.OnTick( delta );
			if( Time > lifeTime )
				Destroy();
		}

		void Destroy()
		{
			SetShouldDetach();

			//go to main menu
			ScreenControlManager.Instance.Controls.Add( new MainMenuWindow() );
		}

		protected override void OnRenderUI( GuiRenderer renderer )
		{
			base.OnRenderUI( renderer );

			Vec2 size = new Vec2( 523.0f / 1024.0f, 235.0f / 768.0f );

			Rect rectangle = new Rect( -size / 2, size / 2 ) + new Vec2( .5f, .5f );

			float alpha = 0;

			if( Time > 2 && Time <= 3 )
				alpha = Time - 2;
			else if( Time > 3 && Time <= lifeTime - 2 - 2 )
				alpha = 1;
			else if( Time >= lifeTime - 2 - 2 && Time < lifeTime - 1 )
				alpha = 1 - ( Time - ( lifeTime - 2 - 2 ) ) / 3;

			if( alpha != 0 )
			{
				renderer.AddQuad( rectangle, new Rect( 0, 0, 1, 1 ), productTexture,
					new ColorValue( 1, 1, 1, alpha ) );
			}
		}

	}
}
