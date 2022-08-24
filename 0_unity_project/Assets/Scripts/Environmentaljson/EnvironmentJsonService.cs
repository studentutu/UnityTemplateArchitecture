using System;
using System.Collections;
using System.Collections.Generic;

[Serializable]
public class EnvironmentJsonService
{
	// Used with OAuth0
	public string base_url_auth;
	public string base_url_backend;
	public string audience;
	public string client_id;
	public string client_secret;
	public string dbConnection;
	public string grant_type;
}