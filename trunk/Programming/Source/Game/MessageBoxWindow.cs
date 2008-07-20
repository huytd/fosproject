// Copyright (C) 2006-2008 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using Engine;
using Engine.UISystem;

namespace Game
{
	/// <summary>
	/// Defines a "MessageBox" window.
	/// </summary>
	public class MessageBoxWindow : EControl
	{
		string messageText;
		string caption;
		EButton.ClickDelegate clickHandler;

		//

		public MessageBoxWindow( string messageText, string caption, EButton.ClickDelegate clickHandler )
		{
			this.messageText = messageText;
			this.caption = caption;
			this.clickHandler = clickHandler;
		}

		protected override void OnAttach()
		{
			base.OnAttach();

			TopMost = true;

			EControl window = ControlDeclarationManager.Instance.CreateControl( 
				"Gui\\MessageBoxWindow.gui" );
			Controls.Add( window );

			window.Controls[ "MessageText" ].Text = messageText;

			window.Text = caption;

			if( clickHandler != null )
				( (EButton)window.Controls[ "OK" ] ).Click += clickHandler;

			BackColor = new Engine.MathEx.ColorValue( 0, 0, 0, .5f );

			EngineApp.Instance.RenderScene();
		}
	}
}
