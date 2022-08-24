## Delete old license :

1) Remove UNITY_LICENSE from gitlab secrets
2) Remove UNITY_PASSWORD, UNITY_USERNAME
3) Go to pipelines -> Clear CI Runners caches
 

## License Activation job (pit-holes) and Pre-requisites:

Branch(es) should be protected to be used with CI/CD

UNITY_PASSWORD 
type variable
unprotected
should not contain any nondigit/symbol characters (forbidden !@#$%^&*() etc.)

UNITY_USERNAME
type variable
variable
unprotected
either username itself or (prefer) e-mail

UNITY_LICENSE
type variable
unprotected
see valid here : ./FromAtrifactCiActivationStage


## Instructions here : https://game.ci/docs/gitlab/activation 

There is always an option to manually activate and export the .alf -> .x.ulf license.
After it they needs to be stored in CI secrets -> 

key:
UNITY_LICENSE

example value (taken from .x.ulf file):

<?xml version="1.0" encoding="UTF-8"?><root>
    <License id="Terms">
        <MachineBindings>
            <Binding Key="1" Value="576562626572264761624c65526f7578"/>
            <Binding Key="2" Value="576562626572264761624c65526f7578"/>
        </MachineBindings>
        <MachineID Value="D7nTUnjNAmtsUMcnoyrqkgIbYdM="/>
        <SerialHash Value="49541d11d74a3f2cef544a475ea0a25953132e90"/>
        <Features>
            <Feature Value="33"/>
            <Feature Value="12"/>
            <Feature Value="34"/>
            <Feature Value="13"/>
            <Feature Value="24"/>
            <Feature Value="25"/>
            <Feature Value="36"/>
            <Feature Value="17"/>
            <Feature Value="18"/>
            <Feature Value="19"/>
            <Feature Value="0"/>
            <Feature Value="1"/>
            <Feature Value="2"/>
            <Feature Value="3"/>
            <Feature Value="4"/>
            <Feature Value="60"/>
            <Feature Value="20"/>
        </Features>
        <DeveloperData Value="AQAAAFNDLUs1Qk4tOVEyQi1FM0Q5LTU3VFktTVlOSw=="/>
        <SerialMasked Value="SC-K5BN-9Q2B-E3D9-57TY-XXXX"/>
        <StartDate Value="2021-07-14T00:00:00"/>
        <StopDate Value="2022-03-24T00:00:00"/>
        <UpdateDate Value="2021-09-10T19:44:44"/>
        <InitialActivationDate Value="2021-07-22T10:22:39"/>
        <LicenseVersion Value="6.x"/>
        <ClientProvidedVersion Value="2020.1.11f1"/>
        <AlwaysOnline Value="false"/>
        <Entitlements>
            <Entitlement Ns="unity_editor" Tag="UnityPersonal" Type="EDITOR" ValidTo="9999-12-31T00:00:00"/>
            <Entitlement Ns="unity_editor" Tag="UnityPersonalPlus" Type="EDITOR" ValidTo="2022-03-22T00:00:00"/>
            <Entitlement Ns="unity_editor" Tag="DarkSkin" Type="EDITOR_FEATURE" ValidTo="9999-12-31T00:00:00"/>
        </Entitlements>
    </License>
<Signature xmlns="http://www.w3.org/2000/09/xmldsig#"><SignedInfo><CanonicalizationMethod Algorithm="http://www.w3.org/TR/2001/REC-xml-c14n-20010315#WithComments"/><SignatureMethod Algorithm="http://www.w3.org/2000/09/xmldsig#rsa-sha1"/><Reference URI="#Terms"><Transforms><Transform Algorithm="http://www.w3.org/2000/09/xmldsig#enveloped-signature"/></Transforms><DigestMethod Algorithm="http://www.w3.org/2000/09/xmldsig#sha1"/><DigestValue>VPeEBxBUIQLQ2qQsVpkNy81q0IA=</DigestValue></Reference></SignedInfo><SignatureValue>h8g0qZHtMWh8JOY9KegLCvKJBBScYT9juUUCPxGR0t23NgpKovZX+PsDSJUW8Zk20EWG/rAqvUTB&#13;
JNkCx6gSXFK9HDLUAg8q/S2J2LU124J6xdqrGw/Yaks3Iz/4krnk5y8CXpFabsR5mIh62N0Q7y+K&#13;
WonC59pGFfEEHyyyhoPttUj3yBYZxWUy8vo+Ry/K/6XPMEZhtoMDMPR911SPSP7WovKmVXqYpxcR&#13;
2bkNyeuj37kyA/HvyRHHEKtBE5aJ3dKE8Wm27T17bxY+8IknGmrbAv8vO/buUvLTC/HKB0RDCNvx&#13;
fDZcdMya9l3s+WYyIAGcSh4K88irOtmgGEEpXg==</SignatureValue></Signature></root>

## Artifact link : https://docs.gitlab.com/ee/api/job_artifacts.html 

IL2CPP job: 
To download :
https://gitlab.com/Virtuix/omni-one/unity-app/vr-launcher/-/jobs/artifacts/develop/download?job=build-android-il2cpp

To Preview :
https://gitlab.com/Virtuix/omni-one/unity-app/vr-launcher/-/jobs/artifacts/develop/browse?job=build-android-il2cpp

Mono job: 
To download :
https://gitlab.com/Virtuix/omni-one/unity-app/vr-launcher/-/jobs/artifacts/develop/download?job=build-android-mono

To Preview :
https://gitlab.com/Virtuix/omni-one/unity-app/vr-launcher/-/jobs/artifacts/develop/browse?job=build-android-mono


## TO manually update it :

Be sure to add a new license to your target account before 
1) Perform deletions
2) Set new UNITY_USERNAME | UNITY_PASSWORD
3) Navigate to CI/CD -> Pipeline -> Run Pipeline 
4) Wait until it finished - it should fail, and will will receive further instructions inside the job logs 
Donwload /.alf and later manually activate inside Unity web page
Go to Account -> Seats to retrieve your pro/target license key.

Be sure to download the activated manual license.
5) Go to Gitlab repo -> Settings -> CI -> add new variable UNITY_LICENSE. 
Put the full content of the .x.ulf (open as notepad and copy full content) int variable content

6) Varify that license is correct by manually running any previous job (for example cache job)