namespace TeltonikaService
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
            this.svcTeltonikaProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.svcTeltonikaInstaller = new System.ServiceProcess.ServiceInstaller();
            // 
            // svcTeltonikaProcessInstaller
            // 
            this.svcTeltonikaProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.svcTeltonikaProcessInstaller.Password = null;
            this.svcTeltonikaProcessInstaller.Username = null;
            // 
            // svcTeltonikaInstaller
            // 
            this.svcTeltonikaInstaller.ServiceName = "TeltonikaService";
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.svcTeltonikaProcessInstaller,
            this.svcTeltonikaInstaller});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller svcTeltonikaProcessInstaller;
        private System.ServiceProcess.ServiceInstaller svcTeltonikaInstaller;
    }
}