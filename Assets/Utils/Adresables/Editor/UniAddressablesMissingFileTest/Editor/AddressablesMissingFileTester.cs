#if UNITASK_ADDRESSABLE_SUPPORT
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;

namespace Kogane.Internal
{
	internal sealed class AddressablesMissingFileTester
	{
		[Category( nameof( Kogane ) )]
		[Test]
		public void Addressable_に_Missing_File_が存在しないか()
		{
			var settings      = AddressableAssetSettingsDefaultObject.Settings;
			var allAssetPaths = new HashSet<string>( AssetDatabase.GetAllAssetPaths() );

			var missingEntries = settings.groups
					.SelectMany( x => x.entries )
					.Where( x => x.address != "Scenes In Build" && x.address != "*/Resources/" && x.address != "Resources" && x.address != "EditorSceneList" )
					.Where( x => !allAssetPaths.Contains( x.AssetPath ) )
					.ToArray()
				;

			if ( missingEntries.Length <= 0 ) return;

			var message = string.Join( "\n", missingEntries.Select( x => $"{x.parentGroup.Name},{x.address}" ) );

			Assert.Fail( message );
		}
	}
}
#endif