using App.Core.CommonPatterns;
using App.Core.MVC.Abstractions;
using App.Core.Tools;

namespace App.Core.Services.AuthMessageBus
{
    /// <summary>
    /// Make sure to initialize it somewhere!
    /// </summary>
    public class AuthEventBus : EventBus
    {
        public AuthEventBus()
        {
            Init();
        }

        public override void Init()
        {
            EventBus.Subscription<DefaultAuthCredentialsBus>()
                .Remove(InvokeProcessing)
                .Add(InvokeProcessing);
            EventBus.Subscription<DefaultAuthQrCodeBus>()
                .Remove(InvokeProcessing)
                .Add(InvokeProcessing);
            EventBus.Subscription<DefaultAuthThirdPartyBus>()
                .Remove(InvokeProcessing)
                .Add(InvokeProcessing);
            EventBus.Subscription<AuthSuccessEventBus>();
            EventBus.Subscription<AuthFailedEventBus>();
            EventBus.Subscription<AuthProcessingEventBus>();
            EventBus.Subscription<AuthLogOutBus>();
        }

        private void InvokeProcessing(object obj)
        {
            InvokeProcessing();
        }

        private void InvokeProcessing(string obj)
        {
            InvokeProcessing();
        }

        private void InvokeProcessing(CredentialsStorageSo.Credential obj)
        {
            InvokeProcessing();
        }

        private void InvokeProcessing()
        {
            EventBus.Subscription<AuthProcessingEventBus>().Send();
        }
    }

    public class AuthProcessingEventBus : EventBus
    {
        public override void Init()
        {
        }
    }

    /// <summary>
    /// Subscribe to AuthSuccessEventBus to get the event if the User is successfully retrieved
    /// Sent this inside a single repository, not inside the provider!
    /// </summary>
    public class AuthSuccessEventBus : EventBus<IUser>
    {
        public override void Init()
        {
        }
    }

    /// <summary>
    /// Subscribe to AuthFailedEventBus to get the event if the Sign In has failed
    /// Sent this inside a single repository, not inside the provider!
    /// </summary>
    public class AuthFailedEventBus : EventBus<string>
    {
        public override void Init()
        {
        }
    }

    /// <summary>
    /// The Default AuthCredentialsEventBus -> Inside default provider-> <see cref="EventBus.Subscribe{AuthCredentialsEventBus}"/>
    /// Prefer to use Only the Provider itself and  AuthSuccessEventBus|AuthFailedEventBus
    /// On Sent -> automatically fires AuthProcessingEventBus
    /// </summary>
    public class DefaultAuthCredentialsBus : EventBus<CredentialsStorageSo.Credential>
    {
        public override void Init()
        {
        }
    }

    /// <summary>
    /// The Default AuthQrCodeEventBus -> Inside default provider-> <see cref="EventBus.Subscribe{AuthQrCodeEventBus}"/>
    /// Prefer to use Only the Provider itself and  AuthSuccessEventBus|AuthFailedEventBus
    /// On Sent -> automatically fires AuthProcessingEventBus
    /// </summary>
    public class DefaultAuthQrCodeBus : EventBus<string>
    {
        public override void Init()
        {
        }
    }

    /// <summary>
    /// The Default AuthThirdPartyEventBus -> Inside default provider-> <see cref="EventBus.Subscribe{AuthThirdPartyEventBus}"/>
    /// Prefer to use Only the Provider itself and  AuthSuccessEventBus|AuthFailedEventBus
    /// On Sent -> automatically fires AuthProcessingEventBus
    /// </summary>
    public class DefaultAuthThirdPartyBus : EventBus<System.Object>
    {
        public override void Init()
        {
        }
    }

    /// <summary>
    /// The Default AuthThirdPartyEventBus -> Inside default provider-> <see cref="EventBus.Subscribe{AuthThirdPartyEventBus}"/>
    /// Prefer to use Only the Provider itself and  AuthSuccessEventBus|AuthFailedEventBus
    /// On Sent -> automatically fires AuthProcessingEventBus
    /// </summary>
    public class AuthLogOutBus : EventBus
    {
        public override void Init()
        {
        }
    }
    
    /// <summary>
    /// Subscribe to AuthSuccessEventBus to get the event if the send email reset password is successfully
    /// Sent this inside a single repository, not inside the provider!
    /// </summary>
    public class AuthSendEmailResetPasswordSuccessEventBus : EventBus<IUser>
    {
        public override void Init()
        {
        }
    }
    
    /// <summary>
    /// Subscribe to AuthSuccessEventBus to get the event send email reset password is failed
    /// Sent this inside a single repository, not inside the provider!
    /// </summary>
    public class AuthSendEmailResetPasswordFailedEventBus : EventBus<string>
    {
        public override void Init()
        {
        }
    }
}