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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager( typeof( MainForm ) );
			this.buttonExit = new System.Windows.Forms.Button();
			this.labelEngineVersion = new System.Windows.Forms.Label();
			this.renderTargetUserControl1 = new WindowsAppFramework.RenderTargetUserControl();
			this.buttonAdditionalForm = new System.Windows.Forms.Button();
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
			// labelEngineVersion
			// 
			this.labelEngineVersion.Anchor = ( (System.Windows.Forms.AnchorStyles)( ( System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left ) ) );
			this.labelEngineVersion.AutoSize = true;
			this.labelEngineVersion.Location = new System.Drawing.Point( 9, 527 );
			this.labelEngineVersion.Name = "labelEngineVersion";
			this.labelEngineVersion.Size = new System.Drawing.Size( 82, 13 );
			this.labelEngineVersion.TabIndex = 4;
			this.labelEngineVersion.Text = "NeoAxis Group Ltd.";
			// 
			// renderTargetUserControl1
			// 
			this.renderTargetUserControl1.Anchor = ( (System.Windows.Forms.AnchorStyles)( ( ( ( System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom )
							| System.Windows.Forms.AnchorStyles.Left )
							| System.Windows.Forms.AnchorStyles.Right ) ) );
			this.renderTargetUserControl1.AutomaticUpdateFPS = 30F;
			this.renderTargetUserControl1.BackColor = System.Drawing.Color.Black;
			this.renderTargetUserControl1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.renderTargetUserControl1.Location = new System.Drawing.Point( 12, 12 );
			this.renderTargetUserControl1.MouseRelativeMode = false;
			this.renderTargetUserControl1.Name = "renderTargetUserControl1";
			this.renderTargetUserControl1.Size = new System.Drawing.Size( 667, 502 );
			this.renderTargetUserControl1.TabIndex = 5;
			// 
			// buttonAdditionalForm
			// 
			this.buttonAdditionalForm.Anchor = ( (System.Windows.Forms.AnchorStyles)( ( System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right ) ) );
			this.buttonAdditionalForm.Location = new System.Drawing.Point( 691, 95 );
			this.buttonAdditionalForm.Name = "buttonAdditionalForm";
			this.buttonAdditionalForm.Size = new System.Drawing.Size( 75, 23 );
			this.buttonAdditionalForm.TabIndex = 6;
			this.buttonAdditionalForm.Text = "New Form";
			this.buttonAdditionalForm.UseVisualStyleBackColor = true;
			this.buttonAdditionalForm.Click += new System.EventHandler( this.buttonAdditionalForm_Click );
			// 
			// MainForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size( 778, 549 );
			this.Controls.Add( this.buttonAdditionalForm );
			this.Controls.Add( this.renderTargetUserControl1 );
			this.Controls.Add( this.labelEngineVersion );
			this.Controls.Add( this.buttonExit );
			this.Icon = ( (System.Drawing.Icon)( resources.GetObject( "$this.Icon" ) ) );
			this.Name = "MainForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Windows Application Example";
			this.FormClosed += new System.Windows.Forms.FormClosedEventHandler( this.MainForm_FormClosed );
			this.Load += new System.EventHandler( this.MainForm_Load );
			this.ResumeLayout( false );
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button buttonExit;
		private System.Windows.Forms.Label labelEngineVersion;
		private WindowsAppFramework.RenderTargetUserControl renderTargetUserControl1;
		private System.Windows.Forms.Button buttonAdditionalForm;

	}
}

