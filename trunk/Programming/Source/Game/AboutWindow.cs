// Copyright (C) 2006-2008 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using Engine;
using Engine.UISystem;
using Engine.MathEx;
using Engine.Renderer;
using GameEntities;

namespace Game
{
	/// <summary>
	/// Defines a about us window.
	/// </summary>
	public class AboutWindow : EControl
	{
		//string lastMusicName;

		protected override void OnAttach()
		{
			base.OnAttach();

			//if( GameMusic.MusicSound != null )
			//   lastMusicName = GameMusic.MusicSound.Name;
			//GameMusic.MusicPlay( "Sounds\\Music\\AboutUs.ogg", false );

			EControl window = ControlDeclarationManager.Instance.CreateControl( "Gui\\AboutWindow.gui" );
			Controls.Add( window );

			window.Controls[ "Version" ].Text = EngineVersionInformation.Version;

			EControl sourceControl = window.Controls[ "Developers" ];

			Font smallFont = FontManager.Instance.LoadFont( "Default", .02f );

			float posY = sourceControl.Position.Value.Y;
			foreach( string str in EngineVersionInformation.Developers )
			{
				ETextBox control = (ETextBox)sourceControl.Clone();
				sourceControl.Parent.Controls.Add( control );
				control.Position = new ScaleValue( ScaleType.Texture, 
					new Vec2( control.Position.Value.X, posY ) );

				control.Text = str;

				if( str.Length > 20 )
				{
					posY += 20;
				}
				else
				{
					posY += 18;
					control.TextColor = new ColorValue( .7f, .7f, .7f );
					control.Font = smallFont;
				}
			}

			sourceControl.SetShouldDetach();
			//Controls.Remove( sourceControl );

			window.Controls[ "Copyright" ].Text = EngineVersionInformation.Copyright;

			window.Controls[ "WWW" ].Text = EngineVersionInformation.WWW;

			( (EButton)window.Controls[ "Quit" ] ).Click += delegate( EButton sender )
			{
				SetShouldDetach();
			};
		}

		protected override void OnDetach()
		{
			//if( lastMusicName != null )
			//   GameMusic.MusicPlay( lastMusicName );

			base.OnDetach();
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
