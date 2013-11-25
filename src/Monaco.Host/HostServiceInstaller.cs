using System.ComponentModel;
using System.Configuration.Install;

namespace Monaco.Host
{
    [RunInstaller(true)]
    public partial class HostServiceInstaller : Installer
    {
        private System.ServiceProcess.ServiceProcessInstaller _serviceProcessInstaller;
        private System.ServiceProcess.ServiceInstaller _serviceInstaller; 

        public HostServiceInstaller()
        {
            InitializeComponent();
        }

        public void Setup(string serviceName, string serviceDescription = null, bool autoStart = false)
        {
            this._serviceProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this._serviceInstaller = new System.ServiceProcess.ServiceInstaller();

            this._serviceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this._serviceProcessInstaller.Password = null;
            this._serviceProcessInstaller.Username = null;

        	this._serviceInstaller.ServiceName = serviceName;
        	this._serviceInstaller.Description = serviceDescription ?? serviceName;

			if (autoStart == true)
			{
				this._serviceInstaller.StartType =
					System.ServiceProcess.ServiceStartMode.Automatic;
			}
			else
			{
				this._serviceInstaller.StartType =
					System.ServiceProcess.ServiceStartMode.Manual;
			}

            this.Installers.AddRange
                (new System.Configuration.Install.Installer[]
                     {
                         this._serviceInstaller
                     });
        }
    }
}