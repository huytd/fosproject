// Copyright (C) 2006-2008 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using Engine;
using Engine.UISystem;
using Engine.MathEx;
using Engine.Renderer;
using GameCommon;
using GameEntities;

namespace Game
{
	/// <summary>
	/// Defines a "PostEffects" window.
	/// </summary>
	public class PostEffectsWindow : EControl
	{
		Viewport viewport;

		EControl window;

		EListBox listBox;

		ECheckBox checkBoxEnabled;
		EScrollBar[] scrollBarFloatParameters = new EScrollBar[ 7 ];
		ECheckBox[] checkBoxBoolParameters = new ECheckBox[ 1 ];

		bool noPostEffectUpdate;

		//

		protected override void OnAttach()
		{
			base.OnAttach();

			viewport = RendererWorld.Instance.DefaultViewport;

			window = ControlDeclarationManager.Instance.CreateControl(
				"Gui\\PostEffectsWindow.gui" );
			Controls.Add( window );

			for( int n = 0; n < scrollBarFloatParameters.Length; n++ )
			{
				scrollBarFloatParameters[ n ] = (EScrollBar)window.Controls[
					"FloatParameter" + n.ToString() ];
				scrollBarFloatParameters[ n ].ValueChange += floatParameter_ValueChange;
			}
			for( int n = 0; n < checkBoxBoolParameters.Length; n++ )
			{
				checkBoxBoolParameters[ n ] = (ECheckBox)window.Controls[ "BoolParameter" + n.ToString() ];
				checkBoxBoolParameters[ n ].CheckedChange += boolParameter_CheckedChange;
			}

			listBox = (EListBox)window.Controls[ "List" ];
			listBox.Items.Add( "HDR" );
			listBox.Items.Add( "Glass" );
			listBox.Items.Add( "OldTV" );
			listBox.Items.Add( "HeatVision" );
			listBox.Items.Add( "MotionBlur" );
			listBox.Items.Add( "RadialBlur" );
			listBox.Items.Add( "Blur" );
			listBox.Items.Add( "Grayscale" );
			listBox.Items.Add( "Invert" );
			listBox.Items.Add( "Tiling" );

			listBox.SelectedIndexChange += listBox_SelectedIndexChange;

			checkBoxEnabled = (ECheckBox)window.Controls[ "Enabled" ];
			checkBoxEnabled.Click += checkBoxEnabled_Click;

			for( int n = 0; n < listBox.Items.Count; n++ )
			{
				EButton itemButton = listBox.ItemButtons[ n ];
				ECheckBox checkBox = (ECheckBox)itemButton.Controls[ "CheckBox" ];

				string name = GetListCompositorItemName( (string)listBox.Items[ n ] );

				CompositorInstance instance = viewport.GetCompositorInstance( name );
				if( instance != null && instance.Enabled )
					checkBox.Checked = true;

				if( itemButton.Text == "HDR" )
					checkBox.Enable = false;

				checkBox.Click += listBoxCheckBox_Click;
			}

			listBox.SelectedIndex = 0;

			( (EButton)window.Controls[ "Close" ] ).Click += delegate( EButton sender )
			{
				SetShouldDetach();
			};

			//ApplyToTheScreenGUI
			{
				ECheckBox checkBox = (ECheckBox)window.Controls[ "ApplyToTheScreenGUI" ];
				checkBox.Checked = EngineApp.Instance.ScreenGuiRenderer.ApplyPostEffectsToScreenRenderer;
				checkBox.Click += delegate( ECheckBox sender )
				{
					EngineApp.Instance.ScreenGuiRenderer.ApplyPostEffectsToScreenRenderer = sender.Checked;
				};
			}

		}

		string GetListCompositorItemName( string itemName )
		{
			return itemName.Split( new char[] { ' ' } )[ 0 ];
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

		void listBox_SelectedIndexChange( EListBox sender )
		{
			UpdateCurrentPostEffectControls();
		}

		void listBoxCheckBox_Click( ECheckBox sender )
		{
			//set listBox current item
			for( int n = 0; n < listBox.Items.Count; n++ )
			{
				EButton itemButton = listBox.ItemButtons[ n ];
				if( itemButton.Controls[ "CheckBox" ] == sender )
					listBox.SelectedIndex = n;
			}

			checkBoxEnabled.Checked = sender.Checked;
			checkBoxEnabled.Enable = ( sender.Parent.Text != "HDR" );

			UpdateCurrentPostEffect();
		}

		void checkBoxEnabled_Click( ECheckBox sender )
		{
			EButton itemButton = listBox.ItemButtons[ listBox.SelectedIndex ];
			( (ECheckBox)itemButton.Controls[ "CheckBox" ] ).Checked = sender.Checked;

			UpdateCurrentPostEffect();
		}

		void floatParameter_ValueChange( EScrollBar sender )
		{
			window.Controls[ sender.Name + "Value" ].Text = sender.Value.ToString( "F2" );
			if( !noPostEffectUpdate )
				UpdateCurrentPostEffect();
		}

		void boolParameter_CheckedChange( ECheckBox sender )
		{
			if( !noPostEffectUpdate )
				UpdateCurrentPostEffect();
		}

		void UpdateCurrentPostEffectControls()
		{
			noPostEffectUpdate = true;

			string name = GetListCompositorItemName( listBox.SelectedItem.ToString() );

			//Hide controls
			{
				for( int n = 0; n < scrollBarFloatParameters.Length; n++ )
				{
					string s = "FloatParameter" + n.ToString();
					window.Controls[ s + "Text" ].Visible = false;
					window.Controls[ s ].Visible = false;
					window.Controls[ s + "Value" ].Visible = false;
					window.Controls[ s + "Value" ].Enable = true;
					window.Controls[ s + "Value" ].Text = "";
				}
				for( int n = 0; n < checkBoxBoolParameters.Length; n++ )
				{
					string s = "BoolParameter" + n.ToString();
					window.Controls[ s ].Visible = false;
					window.Controls[ s ].Enable = true;
				}
				window.Controls[ "Description" ].Text = "";
			}

			//Set post effect name
			window.Controls[ "Name" ].Text = name;

			//Update "Enabled" check box

			EButton itemButton = listBox.ItemButtons[ listBox.SelectedIndex ];
			checkBoxEnabled.Checked = ( (ECheckBox)itemButton.Controls[ "CheckBox" ] ).Checked;
			checkBoxEnabled.Enable = ( itemButton.Text != "HDR" );

			//Show need parameters

			//MotionBlur specific
			if( name == "MotionBlur" )
			{
				window.Controls[ "FloatParameter0Text" ].Visible = true;
				window.Controls[ "FloatParameter0Text" ].Text = "Blur";
				window.Controls[ "FloatParameter0Value" ].Visible = true;
				scrollBarFloatParameters[ 0 ].Visible = true;
				scrollBarFloatParameters[ 0 ].ValueRange = new Range( 0, .97f );

				MotionBlurCompositorInstance instance = (MotionBlurCompositorInstance)
					viewport.GetCompositorInstance( name );
				if( instance != null && instance.Enabled )
					scrollBarFloatParameters[ 0 ].Value = instance.Blur;
				else
					scrollBarFloatParameters[ 0 ].Value = .8f;
			}

			//Blur specific
			if( name == "Blur" )
			{
				window.Controls[ "FloatParameter0Text" ].Visible = true;
				window.Controls[ "FloatParameter0Text" ].Text = "Fuzziness";
				window.Controls[ "FloatParameter0Value" ].Visible = true;
				scrollBarFloatParameters[ 0 ].Visible = true;
				scrollBarFloatParameters[ 0 ].ValueRange = new Range( 0, 15 );

				BlurCompositorInstance instance = (BlurCompositorInstance)
					viewport.GetCompositorInstance( name );
				if( instance != null && instance.Enabled )
					scrollBarFloatParameters[ 0 ].Value = instance.Fuzziness;
				else
					scrollBarFloatParameters[ 0 ].Value = 1;
			}

			//DepthOfField specific
			//if( name == "DepthOfField" )
			//{
			//   window.Controls[ "FloatParameter0Text" ].Visible = true;
			//   window.Controls[ "FloatParameter0Text" ].Text = "Near Depth";
			//   window.Controls[ "FloatParameter0Value" ].Visible = true;

			//   window.Controls[ "FloatParameter1Text" ].Visible = true;
			//   window.Controls[ "FloatParameter1Text" ].Text = "Focal Depth";
			//   window.Controls[ "FloatParameter1Value" ].Visible = true;

			//   window.Controls[ "FloatParameter2Text" ].Visible = true;
			//   window.Controls[ "FloatParameter2Text" ].Text = "Far Depth";
			//   window.Controls[ "FloatParameter2Value" ].Visible = true;

			//   window.Controls[ "FloatParameter3Text" ].Visible = true;
			//   window.Controls[ "FloatParameter3Text" ].Text = "Far Blur Cut";
			//   window.Controls[ "FloatParameter3Value" ].Visible = true;

			//   scrollBarFloatParameters[ 0 ].Visible = true;
			//   scrollBarFloatParameters[ 0 ].ValueRange = new Range( .5f, 30 );
			//   scrollBarFloatParameters[ 1 ].Visible = true;
			//   scrollBarFloatParameters[ 1 ].ValueRange = new Range( 1, 130 );
			//   scrollBarFloatParameters[ 2 ].Visible = true;
			//   scrollBarFloatParameters[ 2 ].ValueRange = new Range( 10, 250 );
			//   scrollBarFloatParameters[ 3 ].Visible = true;
			//   scrollBarFloatParameters[ 3 ].ValueRange = new Range( .1f, 2 );

			//   PostEffects.DepthOfField.CompositorItem item = (PostEffects.DepthOfField.CompositorItem)
			//      viewport.GetCompositorItem( name );
			//   if( item != null && item.Enabled )
			//   {
			//      scrollBarFloatParameters[ 0 ].Value = item.NearDepth;
			//      scrollBarFloatParameters[ 1 ].Value = item.FocalDepth;
			//      scrollBarFloatParameters[ 2 ].Value = item.FarDepth;
			//      scrollBarFloatParameters[ 3 ].Value = item.FarBlurCutoff;
			//   }
			//   else
			//   {
			//      scrollBarFloatParameters[ 0 ].Value = 1;
			//      scrollBarFloatParameters[ 1 ].Value = 10;
			//      scrollBarFloatParameters[ 2 ].Value = 20;
			//      scrollBarFloatParameters[ 3 ].Value = 1;
			//   }
			//}

			//HDR specific
			if( name == "HDR" )
			{
				HDRCompositorInstance instance = (HDRCompositorInstance)
					viewport.GetCompositorInstance( name );

				//Adaptation enable
				window.Controls[ "BoolParameter0" ].Visible = true;
				window.Controls[ "BoolParameter0" ].Text = "Adaptation";
				if( instance != null )
					checkBoxBoolParameters[ 0 ].Checked = instance.Adaptation;

				//AdaptationVelocity
				window.Controls[ "FloatParameter1Text" ].Visible = true;
				window.Controls[ "FloatParameter1Text" ].Text = "Adaptation velocity";
				window.Controls[ "FloatParameter1Value" ].Visible = true;
				scrollBarFloatParameters[ 1 ].Visible = true;
				scrollBarFloatParameters[ 1 ].ValueRange = new Range( .1f, 10 );
				if( instance != null )
					scrollBarFloatParameters[ 1 ].Value = instance.AdaptationVelocity;

				//AdaptationMiddleBrightness
				window.Controls[ "FloatParameter2Text" ].Visible = true;
				window.Controls[ "FloatParameter2Text" ].Text = "Adaptation middle brightness";
				window.Controls[ "FloatParameter2Value" ].Visible = true;
				scrollBarFloatParameters[ 2 ].Visible = true;
				scrollBarFloatParameters[ 2 ].ValueRange = new Range( .1f, 2 );
				if( instance != null )
					scrollBarFloatParameters[ 2 ].Value = instance.AdaptationMiddleBrightness;

				//AdaptationMinimum
				window.Controls[ "FloatParameter3Text" ].Visible = true;
				window.Controls[ "FloatParameter3Text" ].Text = "Adaptation minimum";
				window.Controls[ "FloatParameter3Value" ].Visible = true;
				scrollBarFloatParameters[ 3 ].Visible = true;
				scrollBarFloatParameters[ 3 ].ValueRange = new Range( .1f, 2 );
				if( instance != null )
					scrollBarFloatParameters[ 3 ].Value = instance.AdaptationMinimum;

				//AdaptationMaximum
				window.Controls[ "FloatParameter4Text" ].Visible = true;
				window.Controls[ "FloatParameter4Text" ].Text = "Adaptation maximum";
				window.Controls[ "FloatParameter4Value" ].Visible = true;
				scrollBarFloatParameters[ 4 ].Visible = true;
				scrollBarFloatParameters[ 4 ].ValueRange = new Range( .1f, 2 );
				if( instance != null )
					scrollBarFloatParameters[ 4 ].Value = instance.AdaptationMaximum;

				//BloomBrightThreshold
				window.Controls[ "FloatParameter5Text" ].Visible = true;
				window.Controls[ "FloatParameter5Text" ].Text = "Bloom bright threshold";
				window.Controls[ "FloatParameter5Value" ].Visible = true;
				scrollBarFloatParameters[ 5 ].Visible = true;
				scrollBarFloatParameters[ 5 ].ValueRange = new Range( .1f, 2.0f );
				if( instance != null )
					scrollBarFloatParameters[ 5 ].Value = instance.BloomBrightThreshold;

				//BloomScale
				window.Controls[ "FloatParameter6Text" ].Visible = true;
				window.Controls[ "FloatParameter6Text" ].Text = "Bloom scale";
				window.Controls[ "FloatParameter6Value" ].Visible = true;
				scrollBarFloatParameters[ 6 ].Visible = true;
				scrollBarFloatParameters[ 6 ].ValueRange = new Range( 0, 5 );
				if( instance != null )
					scrollBarFloatParameters[ 6 ].Value = instance.BloomScale;

				if( instance != null )
				{
					for( int n = 1; n <= 4; n++ )
						scrollBarFloatParameters[ n ].Enable = instance.Adaptation;
				}

				window.Controls[ "Description" ].Text = "Use Configurator for enable/disable HDR.\n\n" +
					"Default values are set in \"Data\\Definitions\\Renderer.config\".";
			}

			noPostEffectUpdate = false;
		}

		void UpdateCurrentPostEffect()
		{
			string name = GetListCompositorItemName( listBox.SelectedItem.ToString() );

			bool enabled = checkBoxEnabled.Checked;
			CompositorInstance instance = viewport.GetCompositorInstance( name );

			if( enabled )
			{
				//Enable
				instance = viewport.AddCompositor( name );
				if( instance != null )
					instance.Enabled = true;
			}
			else
			{
				//Disable
				if( name == "MotionBlur" )
				{
					//MotionBlur game specific. No remove compositor. only disable.
					if( instance != null )
						instance.Enabled = false;
				}
				else
					viewport.RemoveCompositor( name );
			}

			//MotionBlur specific
			if( name == "MotionBlur" && instance != null )
			{
				//Disable PlayerCharacter ContusionMotionBlur
				if( PlayerIntellect.Instance != null )
				{
					PlayerCharacter playerCharacter = PlayerIntellect.Instance.
						ControlledObject as PlayerCharacter;
					if( playerCharacter != null )
						playerCharacter.AllowContusionMotionBlur = !enabled;
				}

				if( enabled )
				{
					//Update post effect parameters
					MotionBlurCompositorInstance motionBlurInstance = 
						(MotionBlurCompositorInstance)instance;
					motionBlurInstance.Blur = scrollBarFloatParameters[ 0 ].Value;
				}
			}

			//Blur specific
			if( name == "Blur" && instance != null && enabled )
			{
				//Update post effect parameters
				BlurCompositorInstance motionBlurInstance = (BlurCompositorInstance)instance;
				motionBlurInstance.Fuzziness = scrollBarFloatParameters[ 0 ].Value;
			}

			//DepthOfField specific
			//if( name == "DepthOfField" && item != null )
			//{
			//   if( enabled )
			//   {
			//      //Update post effect parameters
			//      PostEffects.DepthOfField.CompositorItem depthOfFieldItem =
			//         (PostEffects.DepthOfField.CompositorItem)item;

			//      depthOfFieldItem.NearDepth = scrollBarFloatParameters[ 0 ].Value;
			//      depthOfFieldItem.FocalDepth = scrollBarFloatParameters[ 1 ].Value;
			//      depthOfFieldItem.FarDepth = scrollBarFloatParameters[ 2 ].Value;
			//      depthOfFieldItem.FarBlurCutoff = scrollBarFloatParameters[ 3 ].Value;
			//   }
			//}

			//HDR specific
			if( name == "HDR" && instance != null )
			{
				if( enabled )
				{
					//Update post effect parameters
					HDRCompositorInstance hdrInstance = (HDRCompositorInstance)instance;

					hdrInstance.Adaptation = checkBoxBoolParameters[ 0 ].Checked;
					hdrInstance.AdaptationVelocity = scrollBarFloatParameters[ 1 ].Value;
					hdrInstance.AdaptationMiddleBrightness = scrollBarFloatParameters[ 2 ].Value;
					hdrInstance.AdaptationMinimum = scrollBarFloatParameters[ 3 ].Value;
					hdrInstance.AdaptationMaximum = scrollBarFloatParameters[ 4 ].Value;
					hdrInstance.BloomBrightThreshold = scrollBarFloatParameters[ 5 ].Value;
					hdrInstance.BloomScale = scrollBarFloatParameters[ 6 ].Value;

					//Update controls
					for( int n = 1; n <= 4; n++ )
						scrollBarFloatParameters[ n ].Enable = hdrInstance.Adaptation;
				}
			}
		}
	}
}
