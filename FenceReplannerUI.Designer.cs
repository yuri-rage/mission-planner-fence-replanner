namespace FenceReplanner
{
    partial class FenceReplannerUI
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.but_Close = new MissionPlanner.Controls.MyButton();
            this.num_ArcSegmentLength = new System.Windows.Forms.NumericUpDown();
            this.lbl_ArcSegmentLength = new System.Windows.Forms.Label();
            this.lbl_FenceMargin = new System.Windows.Forms.Label();
            this.num_FenceMargin = new System.Windows.Forms.NumericUpDown();
            this.but_TrimPolygon = new MissionPlanner.Controls.MyButton();
            this.but_ReplanMission = new MissionPlanner.Controls.MyButton();
            this.tt_FenceReplanner = new System.Windows.Forms.ToolTip(this.components);
            this.lbl_GitHubLink = new System.Windows.Forms.LinkLabel();
            this.lbl_MinDistance = new System.Windows.Forms.Label();
            this.num_MinDistance = new System.Windows.Forms.NumericUpDown();
            ((System.ComponentModel.ISupportInitialize)(this.num_ArcSegmentLength)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_FenceMargin)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_MinDistance)).BeginInit();
            this.SuspendLayout();
            // 
            // but_Close
            // 
            this.but_Close.Location = new System.Drawing.Point(117, 148);
            this.but_Close.Name = "but_Close";
            this.but_Close.Size = new System.Drawing.Size(88, 23);
            this.but_Close.TabIndex = 3;
            this.but_Close.Text = "Close";
            this.but_Close.TextColorNotEnabled = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(87)))), ((int)(((byte)(4)))));
            this.but_Close.UseVisualStyleBackColor = true;
            this.but_Close.Click += new System.EventHandler(this.but_Close_Click);
            // 
            // num_ArcSegmentLength
            // 
            this.num_ArcSegmentLength.DecimalPlaces = 1;
            this.num_ArcSegmentLength.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.num_ArcSegmentLength.Location = new System.Drawing.Point(150, 17);
            this.num_ArcSegmentLength.Maximum = new decimal(new int[] {
            16,
            0,
            0,
            0});
            this.num_ArcSegmentLength.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.num_ArcSegmentLength.Name = "num_ArcSegmentLength";
            this.num_ArcSegmentLength.Size = new System.Drawing.Size(47, 20);
            this.num_ArcSegmentLength.TabIndex = 5;
            this.num_ArcSegmentLength.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.tt_FenceReplanner.SetToolTip(this.num_ArcSegmentLength, "Target segment length when converting circular fences to polygons");
            this.num_ArcSegmentLength.Value = new decimal(new int[] {
            25,
            0,
            0,
            65536});
            // 
            // lbl_ArcSegmentLength
            // 
            this.lbl_ArcSegmentLength.AutoSize = true;
            this.lbl_ArcSegmentLength.Location = new System.Drawing.Point(20, 19);
            this.lbl_ArcSegmentLength.Name = "lbl_ArcSegmentLength";
            this.lbl_ArcSegmentLength.Size = new System.Drawing.Size(124, 13);
            this.lbl_ArcSegmentLength.TabIndex = 6;
            this.lbl_ArcSegmentLength.Text = "Arc Segment Length (m):";
            this.tt_FenceReplanner.SetToolTip(this.lbl_ArcSegmentLength, "Target segment length when converting circular fences to polygons");
            // 
            // lbl_FenceMargin
            // 
            this.lbl_FenceMargin.AutoSize = true;
            this.lbl_FenceMargin.Location = new System.Drawing.Point(52, 46);
            this.lbl_FenceMargin.Name = "lbl_FenceMargin";
            this.lbl_FenceMargin.Size = new System.Drawing.Size(92, 13);
            this.lbl_FenceMargin.TabIndex = 8;
            this.lbl_FenceMargin.Text = "Fence Margin (m):";
            this.tt_FenceReplanner.SetToolTip(this.lbl_FenceMargin, "Margin by which to plan around fences (can be 0)");
            // 
            // num_FenceMargin
            // 
            this.num_FenceMargin.DecimalPlaces = 1;
            this.num_FenceMargin.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.num_FenceMargin.Location = new System.Drawing.Point(150, 44);
            this.num_FenceMargin.Maximum = new decimal(new int[] {
            16,
            0,
            0,
            0});
            this.num_FenceMargin.Name = "num_FenceMargin";
            this.num_FenceMargin.Size = new System.Drawing.Size(47, 20);
            this.num_FenceMargin.TabIndex = 7;
            this.num_FenceMargin.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.tt_FenceReplanner.SetToolTip(this.num_FenceMargin, "Margin by which to plan around fences (can be 0)");
            this.num_FenceMargin.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // but_TrimPolygon
            // 
            this.but_TrimPolygon.Location = new System.Drawing.Point(15, 110);
            this.but_TrimPolygon.Name = "but_TrimPolygon";
            this.but_TrimPolygon.Size = new System.Drawing.Size(88, 23);
            this.but_TrimPolygon.TabIndex = 9;
            this.but_TrimPolygon.Text = "Trim Polygon";
            this.but_TrimPolygon.TextColorNotEnabled = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(87)))), ((int)(((byte)(4)))));
            this.tt_FenceReplanner.SetToolTip(this.but_TrimPolygon, "Trim polygon edges around intersecting fences");
            this.but_TrimPolygon.UseVisualStyleBackColor = true;
            // 
            // but_ReplanMission
            // 
            this.but_ReplanMission.Location = new System.Drawing.Point(117, 110);
            this.but_ReplanMission.Name = "but_ReplanMission";
            this.but_ReplanMission.Size = new System.Drawing.Size(88, 23);
            this.but_ReplanMission.TabIndex = 10;
            this.but_ReplanMission.Text = "Replan Mission";
            this.but_ReplanMission.TextColorNotEnabled = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(87)))), ((int)(((byte)(4)))));
            this.tt_FenceReplanner.SetToolTip(this.but_ReplanMission, "Replan mission waypoints around fences");
            this.but_ReplanMission.UseVisualStyleBackColor = true;
            // 
            // lbl_GitHubLink
            // 
            this.lbl_GitHubLink.AutoSize = true;
            this.lbl_GitHubLink.Location = new System.Drawing.Point(12, 145);
            this.lbl_GitHubLink.Name = "lbl_GitHubLink";
            this.lbl_GitHubLink.Size = new System.Drawing.Size(89, 26);
            this.lbl_GitHubLink.TabIndex = 11;
            this.lbl_GitHubLink.TabStop = true;
            this.lbl_GitHubLink.Text = "Fence Replanner\r\nDocumentation";
            this.lbl_GitHubLink.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lbl_GitHubLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lbl_GitHubLink_LinkClicked);
            // 
            // lbl_MinDistance
            // 
            this.lbl_MinDistance.AutoSize = true;
            this.lbl_MinDistance.Location = new System.Drawing.Point(34, 72);
            this.lbl_MinDistance.Name = "lbl_MinDistance";
            this.lbl_MinDistance.Size = new System.Drawing.Size(110, 13);
            this.lbl_MinDistance.TabIndex = 13;
            this.lbl_MinDistance.Text = "Min WP Distance (m):";
            this.tt_FenceReplanner.SetToolTip(this.lbl_MinDistance, "Remove consecutive waypoints within this distance of each other");
            // 
            // num_MinDistance
            // 
            this.num_MinDistance.DecimalPlaces = 2;
            this.num_MinDistance.Increment = new decimal(new int[] {
            1,
            0,
            0,
            131072});
            this.num_MinDistance.Location = new System.Drawing.Point(150, 70);
            this.num_MinDistance.Maximum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.num_MinDistance.Name = "num_MinDistance";
            this.num_MinDistance.Size = new System.Drawing.Size(47, 20);
            this.num_MinDistance.TabIndex = 12;
            this.num_MinDistance.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.tt_FenceReplanner.SetToolTip(this.num_MinDistance, "Remove consecutive waypoints within this distance of each other");
            this.num_MinDistance.Value = new decimal(new int[] {
            5,
            0,
            0,
            65536});
            // 
            // FenceReplannerUI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(220, 190);
            this.Controls.Add(this.lbl_MinDistance);
            this.Controls.Add(this.num_MinDistance);
            this.Controls.Add(this.lbl_GitHubLink);
            this.Controls.Add(this.but_ReplanMission);
            this.Controls.Add(this.but_TrimPolygon);
            this.Controls.Add(this.lbl_FenceMargin);
            this.Controls.Add(this.num_FenceMargin);
            this.Controls.Add(this.lbl_ArcSegmentLength);
            this.Controls.Add(this.num_ArcSegmentLength);
            this.Controls.Add(this.but_Close);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FenceReplannerUI";
            this.ShowIcon = false;
            this.Text = "Fence Replanner";
            this.Load += new System.EventHandler(this.FenceReplannerUI_Load);
            ((System.ComponentModel.ISupportInitialize)(this.num_ArcSegmentLength)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_FenceMargin)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_MinDistance)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private MissionPlanner.Controls.MyButton but_Close;
        private System.Windows.Forms.Label lbl_ArcSegmentLength;
        private System.Windows.Forms.Label lbl_FenceMargin;
        public System.Windows.Forms.NumericUpDown num_ArcSegmentLength;
        public System.Windows.Forms.NumericUpDown num_FenceMargin;
        private System.Windows.Forms.ToolTip tt_FenceReplanner;
        public MissionPlanner.Controls.MyButton but_TrimPolygon;
        public MissionPlanner.Controls.MyButton but_ReplanMission;
        private System.Windows.Forms.LinkLabel lbl_GitHubLink;
        private System.Windows.Forms.Label lbl_MinDistance;
        public System.Windows.Forms.NumericUpDown num_MinDistance;
    }
}