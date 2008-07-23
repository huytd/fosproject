namespace WindowsAppExample
{
	partial class MainForm
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose( bool disposing )
		{
			if( disposing && ( components != null ) )
			{
				components.Dispose();
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager( typeof( MainForm ) );
			this.buttonExit = new System.Windows.Forms.Button();
			this.panelEngine = new System.Windows.Forms.Panel();
			this.timer1 = new System.Windows.Forms.Timer( this.components );
			this.trackBarSoundVolume = new System.Windows.Forms.TrackBar();
			this.label1 = new System.Windows.Forms.Label();
			this.labelEngineVersion = new System.Windows.Forms.Label();
			( (System.ComponentModel.ISupportInitialize)( this.trackBarSoundVolume ) ).BeginInit();
			this.SuspendLayout();
			// 
			// buttonExit
			// 
			this.buttonExit.Anchor = ( (System.Windows.Forms.AnchorStyles)( ( System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right ) ) );
			this.buttonExit.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.buttonExit.Location = new System.Drawing.Point( 691, 12 );
			this.buttonExit.Name = "buttonExit";
			this.buttonExit.Size = new System.Drawing.Size( 75, 23 );
			this.buttonExit.TabIndex = 0;
			this.buttonExit.Text = "E&xit";
			this.buttonExit.UseVisualStyleBackColor = true;
			this.buttonExit.Click += new System.EventHandler( this.buttonExit_Click );
			// 
			// panelEngine
			// 
			this.panelEngine.Anchor = ( (System.Windows.Forms.AnchorStyles)( ( ( ( System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom )
							| System.Windows.Forms.AnchorStyles.Left )
							| System.Windows.Forms.AnchorStyles.Right ) ) );
			this.panelEngine.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.panelEngine.Location = new System.Drawing.Point( 12, 12 );
			this.panelEngine.Name = "panelEngine";
			this.panelEngine.Size = new System.Drawing.Size( 663, 504 );
			this.panelEngine.TabIndex = 1;
			this.panelEngine.Paint += new System.Windows.Forms.PaintEventHandler( this.panelEngine_Paint );
			// 
			// timer1
			// 
			this.timer1.Interval = 15;
			this.timer1.Tick += new System.EventHandler( this.timer1_Tick );
			// 
			// trackBarSoundVolume
			// 
			this.trackBarSoundVolume.Anchor = ( (System.Windows.Forms.AnchorStyles)( ( System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right ) ) );
			this.trackBarSoundVolume.AutoSize = false;
			this.trackBarSoundVolume.LargeChange = 50;
			this.trackBarSoundVolume.Location = new System.Drawing.Point( 681, 78 );
			this.trackBarSoundVolume.Maximum = 1000;
			this.trackBarSoundVolume.Name = "trackBarSoundVolume";
			this.trackBarSoundVolume.Size = new System.Drawing.Size( 95, 28 );
			this.trackBarSoundVolume.SmallChange = 10;
			this.trackBarSoundVolume.TabIndex = 2;
			this.trackBarSoundVolume.TickFrequency = 500;
			this.trackBarSoundVolume.Scroll += new System.EventHandler( this.trackBarSoundVolume_Scroll );
			// 
			// label1
			// 
			this.label1.Anchor = ( (System.Windows.Forms.AnchorStyles)( ( System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right ) ) );
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point( 688, 62 );
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size( 78, 13 );
			this.label1.TabIndex = 3;
			this.label1.Text = "Sound volume:";
			// 
			// labelEngineVersion
			// 
			this.labelEngineVersion.Anchor = ( (System.Windows.Forms.AnchorStyles)( ( System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left ) ) );
			this.labelEngineVersion.AutoSize = true;
			this.labelEngineVersion.Location = new System.Drawing.Point( 9, 527 );
			this.labelEngineVersion.Name = "labelEngineVersion";
			this.labelEngineVersion.Size = new System.Drawing.Size( 82, 13 );
			this.labelEngineVersion.TabIndex = 4;
			this.labelEngineVersion.Text = "NeoAxis Engine";
			// 
			// MainForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size( 778, 549 );
			this.Controls.Add( this.labelEngineVersion );
			this.Controls.Add( this.label1 );
			this.Controls.Add( this.trackBarSoundVolume );
			this.Controls.Add( this.panelEngine );
			this.Controls.Add( this.buttonExit );
			this.Icon = ( (System.Drawing.Icon)( resources.GetObject( "$this.Icon" ) ) );
			this.Name = "MainForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Windows Application Example";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler( this.MainForm_FormClosing );
			this.Load += new System.EventHandler( this.MainForm_Load );
			( (System.ComponentModel.ISupportInitialize)( this.trackBarSoundVolume ) ).EndInit();
			this.ResumeLayout( false );
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button buttonExit;
		private System.Windows.Forms.Panel panelEngine;
		private System.Windows.Forms.Timer timer1;
		private System.Windows.Forms.TrackBar trackBarSoundVolume;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label labelEngineVersion;

	}
}

