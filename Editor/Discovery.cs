/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Unity Technologies.
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System.Collections.Generic;
using System.IO;
using Unity.VoidEditor.Editor; // Namespace for VoidEditorInstallation

namespace Microsoft.Unity.VisualStudio.Editor
{
	internal static class Discovery
	{
		public static IEnumerable<IVisualStudioInstallation> GetVisualStudioInstallations()
		{
			// Keep existing discoveries if they are still relevant
			foreach (var installation in VisualStudioCursorInstallation.GetVisualStudioInstallations())
				yield return installation;
			foreach (var installation in VisualStudioCodiumInstallation.GetVisualStudioInstallations())
				yield return installation;
			
			// Add Void Editor discovery
			foreach (var installation in VoidEditorInstallation.GetInstallations())
				yield return installation; // Now compatible as VoidEditorInstallation implements IVisualStudioInstallation
		}

		public static bool TryDiscoverInstallation(string editorPath, out IVisualStudioInstallation installation)
		{
			// Try existing ones first
			try
			{
				if (VisualStudioCursorInstallation.TryDiscoverInstallation(editorPath, out installation))
					return true;
				if (VisualStudioCodiumInstallation.TryDiscoverInstallation(editorPath, out installation))
					return true;
			}
			catch (IOException)
			{
				// If one discovery fails, still try others.
				installation = null;
			}

			// Try Void Editor
			// The out parameter of VoidEditorInstallation.TryDiscoverInstallation is VoidEditorInstallation.
			// It can be directly assigned to IVisualStudioInstallation if the class implements the interface.
			if (VoidEditorInstallation.TryDiscoverInstallation(editorPath, out VoidEditorInstallation voidInstallation))
			{
				installation = voidInstallation;
				return true;
			}
			
			installation = null; // Ensure installation is null if nothing is found
			return false;
		}

		public static void Initialize()
		{
            VisualStudioCursorInstallation.Initialize();
            VisualStudioCodiumInstallation.Initialize();
            VoidEditorInstallation.Initialize(); // Initialize Void Editor discovery
		}
	}
}
