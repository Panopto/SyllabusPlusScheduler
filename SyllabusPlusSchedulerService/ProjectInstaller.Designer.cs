namespace SyllabusPlusSchedulerService
{
    partial class ProjectInstaller
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

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.ScheduleRecordingProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.PanoptoScheduleRecordingServiceInstaller = new System.ServiceProcess.ServiceInstaller();
            // 
            // ScheduleRecordingProcessInstaller
            // 
            this.ScheduleRecordingProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.ScheduleRecordingProcessInstaller.Password = null;
            this.ScheduleRecordingProcessInstaller.Username = null;
            // 
            // PanoptoScheduleRecordingServiceInstaller
            // 
            this.PanoptoScheduleRecordingServiceInstaller.Description = "Schedules Remote Recordings";
            this.PanoptoScheduleRecordingServiceInstaller.DisplayName = "Schedule Recording Service";
            this.PanoptoScheduleRecordingServiceInstaller.ServiceName = "Panopto Schedule Recording Service";
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.ScheduleRecordingProcessInstaller,
            this.PanoptoScheduleRecordingServiceInstaller});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller ScheduleRecordingProcessInstaller;
        private System.ServiceProcess.ServiceInstaller PanoptoScheduleRecordingServiceInstaller;
    }
}