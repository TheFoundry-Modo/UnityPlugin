/*
 *   Copyright 2016 The Foundry Visionmongers Ltd.
 *
 *   Licensed under the Apache License, Version 2.0 (the "License");
 *   you may not use this file except in compliance with the License.
 *   You may obtain a copy of the License at
 *
 *       http://www.apache.org/licenses/LICENSE-2.0
 *
 *   Unless required by applicable law or agreed to in writing, software
 *   distributed under the License is distributed on an "AS IS" BASIS,
 *   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *   See the License for the specific language governing permissions and
 *   limitations under the License.
 */
 
﻿#if UNITY_5_0 || UNITY_5_3_OR_NEWER
#define MODO_COMPATIBLE_UNITY_VERSION
#else
#undef MODO_COMPATIBLE_UNITY_VERSION
#endif

using UnityEditor;
using UnityEngine;
using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Reflection;


#if MODO_COMPATIBLE_UNITY_VERSION
/*
 * RESULTING TEXTURE HOLDER
 */
public class MODOTexture
{
	public Material material;
	public Texture texture;
	public string path;
	public string slot;
	public string name;
	public MODOChannel channel;
	public MODOColorspace colorspace;

	public MODOTexture (
		Material material = null,
		Texture texture = null,
		string path = null,
		string slot = null,
		string name = null,
		MODOChannel channel = MODOChannel.RGB,
		MODOColorspace colorspace = MODOColorspace.sRGB)
	{
		this.material = material;
		this.texture = texture;
		this.path = path;
		this.slot = slot;
		this.name = name;
		this.channel = channel;
		this.colorspace = colorspace;
	}
}

/*
* PENDING TEXTURE CLASSES
*/

// UV Maps.
public enum MODOChannel
{
	RGB,
	Red,
	Green,
	Blue,
	Alpha
}

// UV Maps.
public enum MODOUVMap
{
	UV1,
	UV2
}

// Colorspaces.
public enum MODOColorspace
{
	sRGB,
	linear
}

// States for pending textures.
public enum MODOPendingState
{
	Pending,
	ReadyToApply,
	Applied
}

// Holds the information for a pending texture.
public class MODOMaterialPendingTexture
{
	//public Material material;
	public MODOTexture texture;
	public MODOPendingState state = MODOPendingState.Pending;
}

/* Holds the pending textures - textures that have been copied
 * into the texture directory at the time of material creation.
 * These get applied after they've been imported.
 */
public class MODOMaterialPendingTextureContainer
{
	// All the pending textures.
	List<MODOMaterialPendingTexture> pending = new List<MODOMaterialPendingTexture>();

	// Predicate to find pending textures that are ready to apply.
	static bool isReadyToApply(MODOMaterialPendingTexture obj)
	{
		return (obj.state == MODOPendingState.ReadyToApply);
	}

	// Predicate to find pending textures that were applied, successful or otherwise.
	static bool applied(MODOMaterialPendingTexture obj)
	{
		return (obj.state == MODOPendingState.Applied);
	}

	// Add a new pending texture.
	// Only if it hasn't been added already.
	// Force may be set to true to overwrite an existing pending texture if it's already added.
	public bool addPendingTexture(Material material, MODOTexture texture, bool force = false)
	{
		int index = pending.FindIndex(pendingTexture => ((material == pendingTexture.texture.material) && (texture.slot == pendingTexture.texture.slot)));
		if (index == -1)
		{
			MODOMaterialPendingTexture newPending = new MODOMaterialPendingTexture();
			newPending.texture = texture;
			pending.Add(newPending);
			return true;
		}
		else if (force)
		{
			pending[index].texture = texture;
			return true;
		}
		return false;
	}

	// Get all the pending textures with this texture path.
	public List<MODOMaterialPendingTexture> getPendingTextures(string texturePath)
	{
		return pending.FindAll(pendingTexture => (texturePath == pendingTexture.texture.path));
	}

	// Get all the pending textures with this material.
	public List<MODOMaterialPendingTexture> getPendingTextures(Material material)
	{
		return pending.FindAll(pendingTexture => (material == pendingTexture.texture.material));
	}

	// Get all the pending textures with this material.
	public List<MODOMaterialPendingTexture> getPendingTextures()
	{
		return pending;
	}

	// Apply all the newly imported textures to their materials.
	public void applyTexture()
	{
		foreach (MODOMaterialPendingTexture pendingTexture in pending.FindAll(isReadyToApply))
		{
			if (pendingTexture.texture.material != null)
			{
				Texture texture = AssetDatabase.LoadAssetAtPath(pendingTexture.texture.path, typeof(Texture)) as Texture;
				if (texture != null)
				{
					pendingTexture.texture.material.SetTexture(pendingTexture.texture.slot, texture);
					EditorUtility.SetDirty(pendingTexture.texture.material);

					// If this is getting applied as a normal map, enable normal mapping.
					if ((pendingTexture.texture.slot == "_BumpMap") || (pendingTexture.texture.slot == "_DetailNormalMap"))
					{
						pendingTexture.texture.material.EnableKeyword("_NORMALMAP");
					}

					// If this is getting applied as a parallax map, enable parallax mapping.
					if (pendingTexture.texture.slot == "_ParallaxMap")
					{
						pendingTexture.texture.material.EnableKeyword("_PARALLAXMAP");
					}

					// If this is getting applied as a parallax map, enable parallax mapping.
					if (pendingTexture.texture.slot == "_ParallaxMap")
					{
						pendingTexture.texture.material.EnableKeyword("_PARALLAXMAP");
					}

					// If this is getting applied as a specular map, enable specular workflow.
					if (pendingTexture.texture.slot == "_SpecGlossMap")
					{
						pendingTexture.texture.material.EnableKeyword("_SPECGLOSSMAP");
					}

					// If this is getting applied as a metallic map, enable metallic workflow.
					if (pendingTexture.texture.slot == "_MetallicGlossMap")
					{
						pendingTexture.texture.material.EnableKeyword("_METALLICGLOSSMAP");
					}

					// If this is getting applied as a detail map, enable detail mapping.
					if ((pendingTexture.texture.slot == "_DetailAlbedoMap") || (pendingTexture.texture.slot == "_DetailNormalMap"))
					{
						pendingTexture.texture.material.EnableKeyword("_DETAIL_MULX2");
					}
				}
			}
			pendingTexture.state = MODOPendingState.Applied;
		}

		pending.RemoveAll(applied);

		AssetDatabase.Refresh();
	}
}
#endif


/*
* XML SPECIFICATION
*/


// Holds the information about a material property's texture.
// Loaded from XML.
public class MODOMaterialPropertyTexture
{
	[XmlAttribute("name")]
	public string name = null;

	[XmlAttribute("filename")]
	public string filename = null;

	[XmlAttribute("channel")]
	public MODOChannel channel = MODOChannel.RGB;

	[XmlAttribute("wrapU")]
	public float wrapU = 1.0f;

	[XmlAttribute("wrapV")]
	public float wrapV = 1.0f;

	[XmlAttribute("uvmap")]
	public MODOUVMap uvmap = MODOUVMap.UV1;

	[XmlAttribute("fileIndex")]
	public int imageIndex = -1;

	[XmlAttribute("uvindex")]
	public int uvIndex = -1;

	[XmlAttribute("uvname")]
	public string uvName = null;

#if MODO_COMPATIBLE_UNITY_VERSION
	public Vector2 getWrapUV(Vector2 inScale)
	{
		return new Vector2(wrapU, wrapV);
	}
#endif
}

// Holds the information about a material's property (values and a list of textures, if any).
// Loaded from XML.
public class MODOMaterialProperty
{
	[XmlAttribute("name")]
	public string name;

	[XmlAttribute("value")]
	public string value_input
	{
		set
		{
			this.value = value;
			vector = Vector4.zero;
			float[] values = new float[4];
			vectorCount = 0;
			MODOMaterialImporter.parseNumericValue(value, values, out vectorCount);
			for (int i = 0; i < vectorCount; i++)
			{
				vector[i] = values[i];
			}
		}
		get
		{
			return value;
		}
	}

	[XmlIgnore]
	public string value;
	[XmlIgnore]
	public Vector4 vector = Vector4.zero;
	[XmlIgnore]
	public int vectorCount = 0;

	[XmlElement("texture")]
	public List<MODOMaterialPropertyTexture> textures = new List<MODOMaterialPropertyTexture>();

#if MODO_COMPATIBLE_UNITY_VERSION
	public MODOMaterialPropertyTexture getTexture(string filename)
	{
		return textures.Find(property => property.filename == filename);
	}

	public int numTextures()
	{
		return textures.Count();
	}
#endif
}

// Holds a list of all the texture files used in the material.
// Loaded from XML.
public class MODOImageContainer
{
	[XmlElement("file")]
	public List<MODOImage> files = new List<MODOImage>();
}

// Holds the information about the images used by the materials.
// Loaded from XML.
public class MODOImage
{
	[XmlAttribute("color_correction")]
	public MODOColorspace colorspace;

	[XmlAttribute("filename")]
	public string file;
}

// Holds the information about a material as well as it's properties.
// Loaded from XML.
public class MODOMaterial
{
	[XmlAttribute("ID")]
	public string ID;

	[XmlAttribute("ptag")]
	public string name;

	[XmlAttribute("type")]
	public string type;

	[XmlElement("property")]
	public List<MODOMaterialProperty> properties = new List<MODOMaterialProperty>();


#if MODO_COMPATIBLE_UNITY_VERSION
	public MODOMaterialProperty getProperty(string name)
	{
		return properties.Find(property => property.name == name);
	}

	// Return the diffuse color.
	public Color getDiffuseColor()
	{
		Color diffuseColor = Color.white;

		MODOMaterialProperty diffuseColorProperty = properties.Find(property => property.name == "Albedo");
		if (diffuseColorProperty != null)
		{
			if (diffuseColorProperty.vectorCount == 3)
			{
				diffuseColor.r = diffuseColorProperty.vector.x;
				diffuseColor.g = diffuseColorProperty.vector.y;
				diffuseColor.b = diffuseColorProperty.vector.z;
			}
		}

		// Stick the opacity level into the alpha channel.
		MODOMaterialProperty opacityProperty = properties.Find(property => property.name == "Opacity");
		if (opacityProperty != null)
		{
			if (opacityProperty.vectorCount == 1)
			{
				diffuseColor.a = opacityProperty.vector.x;
			}
		}

		return diffuseColor;
	}

	// Return the emissive color.
	public Color getEmissiveColor()
	{
		// Get the color.
		Color emissiveColor = Color.black;

		MODOMaterialProperty emissiveColorProperty = properties.Find(property => property.name == "Emission");
		if (emissiveColorProperty != null)
		{
			if (emissiveColorProperty.vectorCount == 3)
			{
				emissiveColor.r = emissiveColorProperty.vector.x;
				emissiveColor.g = emissiveColorProperty.vector.y;
				emissiveColor.b = emissiveColorProperty.vector.z;
			}
		}

		// Multiply the emissive color by the emissive level.
		MODOMaterialProperty emissiveLevelProperty = properties.Find(property => property.name == "Emissive Level");
		if (emissiveLevelProperty != null)
		{
			if (emissiveLevelProperty.vectorCount == 1)
			{
				emissiveColor *= emissiveLevelProperty.vector.x;
			}
		}

		return emissiveColor;
	}
#endif
}

// Holds the information about the version of MODO and exporter this XML came from.
// Loaded from XML.
public class MODOMaterialVersion
{
	[XmlAttribute("app")]
	public string app; // Application the file came from.

	[XmlAttribute("build")]
	public int build; // Application build number the file came from.

	[XmlAttribute("xml_file_format")]
	public int versionXML = 0; // Version of the XML format.

	[XmlText]
	public int version; // Application version the file came from.
}

// Holds the materials in this asset.
[XmlRoot("catalog")]
public class MODOMaterialContainer
{
	[XmlElement("useRootPath")]
	public int useRootPath;

	[XmlElement("RootPath")]
	public string rootPath;

	[XmlElement("Version")]
	public MODOMaterialVersion MODOversion;

	[XmlElement("Material")]
	public List<MODOMaterial> materials = new List<MODOMaterial>();

	[XmlElement("ImageFiles")]
	public MODOImageContainer images = new MODOImageContainer();

	public MODOMaterial getMaterial(string name)
	{
		return materials.Find(mat => mat.name == name);
	}

	public bool useRoot()
	{
		if (useRootPath == 1)
		{
			return (!string.IsNullOrEmpty(rootPath));
		}
		return false;
	}
}

/*
* ASSET POSTPROCESSOR
*/

public class MODOMaterialImporter : AssetPostprocessor
{

	/*
	* XML LOADERS
	*/

	static MODOMaterialContainer LoadMaterialXMLFromStringReader(StringReader stringReader)
	{
		MODOMaterialContainer matContainer = null;
		XmlSerializer serializer = new XmlSerializer(typeof(MODOMaterialContainer));

		//serializer.UnknownAttribute += new XmlAttributeEventHandler(MODOMaterialImporter.Serializer_UnknownAttribute);

		try
		{
			matContainer = serializer.Deserialize(stringReader) as MODOMaterialContainer;
		}
		catch (System.Exception e)
		{
			if (e != null) { /*DebugLog(e.ToString());*/ } // Stop Unity complaining about unused variables.
			matContainer = null;
		}
		return matContainer;
	}

	static MODOMaterialContainer LoadMaterialXMLFromString(string content)
	{
		MODOMaterialContainer matContainer = null;
		if (!string.IsNullOrEmpty(content))
		{
			matContainer = LoadMaterialXMLFromStringReader(new StringReader(content));
		}
		return matContainer;
	}

	static MODOMaterialContainer LoadMaterialXMLFromPath(string path)
	{
		MODOMaterialContainer matContainer = null;
		TextAsset xmlAsset = AssetDatabase.LoadAssetAtPath(path, typeof(TextAsset)) as TextAsset;
		if ((xmlAsset != null) && !string.IsNullOrEmpty(xmlAsset.text))
		{
			matContainer = LoadMaterialXMLFromStringReader(new StringReader(xmlAsset.text));
		}
		return matContainer;
	}

#if MODO_COMPATIBLE_UNITY_VERSION


	/*
	* GLOBALS AND CONSTANTS
	*/

	string projectPath = Directory.GetCurrentDirectory();
	const string materialDir = "Materials";
	const string textureDir = "Textures";
	const string shaderName = "Standard";


	/*
	* DEFAULT PREFERENCES
	*/

	static bool pref_alwaysApply = EditorPrefs.GetBool("MODOMaterialAlwaysApply", true);
	static bool pref_alwaysImport = EditorPrefs.GetBool("MODOMaterialAlwaysImport", false);
	static bool pref_debug = EditorPrefs.GetBool("MODOMaterialDebug", false);
	static bool pref_showWarnings = EditorPrefs.GetBool("MODOMaterialShowWarnings", true);
	static void DebugLog(string message)
	{
		if (pref_debug)
		{
			Debug.Log("MODO Material Importer\n" + message);
		}
	}
	static void DebugWarning(string message)
	{
		Debug.LogWarning("MODO Material Importer\n" + message + "\n");
	}


	/*
	* UNITY VERSION CHECK
	*/

	// Check which version of Unity this is.
	static int UnityVersionMajor()
	{
		int majorVer = 0;
		string[] verStr = Application.unityVersion.Split('.');
		if ((verStr.Length > 0) && int.TryParse(verStr[0], out majorVer))
		{
			return majorVer;
		}
		return majorVer;
	}

	static int UnityVersionMinor()
	{
		int minorVer = 0;
		string[] verStr = Application.unityVersion.Split('.');
		if ((verStr.Length > 1) && int.TryParse(verStr[1], out minorVer))
		{
			return minorVer;
		}
		return minorVer;
	}

	static bool isAtLeastUnity5 = (UnityVersionMajor() >= 5);
	static bool isAtLeastUnity5_4 = ((UnityVersionMajor() >= 5) && (UnityVersionMinor() >= 4));

	static MODOMaterialPendingTextureContainer pendingTextures = new MODOMaterialPendingTextureContainer();



	/*
	* MATERIAL PARAMETER MAPPING
	*/

	// Structure for holding the mapping from MODO Material parameters to Unity material slots.
	// Null value for tSlot, sSlot means it shouldn't be applied there.
	// If both are present, then sSlot gets filled only if a texture isn't found.
	// Null texture channel means it wants to read from RGBA so specific channel isn't required.
	struct MaterialParameter
	{
		public string name;         // MODO material parameter name.
		public string tSlot;      // Shader parameter the texture should go into.
		public string sSlot;        // Shader parameter the numeric value should go into.
		public MODOChannel channel;   // Channel of the texture that Unity will attempt to read from.

		public MaterialParameter(
			string name = null,
			string tSlot = null,
			string sSlot = null,
			MODOChannel channel = MODOChannel.RGB)
		{
			this.name = name;
			this.tSlot = tSlot;
			this.sSlot = sSlot;
			this.channel = channel;
		}
	}

	// Map MODO material properties to Unity shader slots.
	static readonly List<MaterialParameter> materialParams = new List<MaterialParameter>
	(new[] {
			//						MODO Parameter Name		Unity Texture Slot		Unity Scalar Slot			Texture Channel
			new MaterialParameter ("Albedo",                "_MainTex",             null,                       MODOChannel.RGB),
			new MaterialParameter ("Normal",                "_BumpMap",             null,                       MODOChannel.RGB),
			new MaterialParameter ("Normal Scale",          null,                   "_BumpScale",               MODOChannel.RGB),
			new MaterialParameter ("Ambient Occlusion",     "_OcclusionMap",        null,                       MODOChannel.Green),
			new MaterialParameter ("Emission",              "_EmissionMap",         null,                       MODOChannel.RGB),
			//Emissive level is pre-multiplied into the emission color further up.
			//new MaterialParameter ("Emissive Level",		null,					"_EmissionColor",			MODOChannel.RGB),
			new MaterialParameter ("Detail Mask",           "_DetailMask",          null,                       MODOChannel.Alpha),
			new MaterialParameter ("Detail Albedo x2",      "_DetailAlbedoMap",     null,                       MODOChannel.RGB),
			new MaterialParameter ("Detail Normal",         "_DetailNormalMap",     null,                       MODOChannel.RGB),
			new MaterialParameter ("Detail Normal Scale",   null,                   "_DetailNormalMapScale",    MODOChannel.RGB),
			new MaterialParameter ("Metallic",              "_MetallicGlossMap",    "_Metallic",                MODOChannel.Red),
			new MaterialParameter ("Bump",                  "_ParallaxMap",         null,                       MODOChannel.Green),
			new MaterialParameter ("Height Scale",          null,                   "_Parallax",                MODOChannel.RGB),
			new MaterialParameter ("Smoothness",            "_MetallicGlossMap",    "_Glossiness",              MODOChannel.Alpha)
	});



	/*
	* FILE PATH HANDLING
	*/

	public static string NormalizePath(string path)
	{
		return path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
	}

	// Given a directory path, return a list of all of the directories leading up to it.
	// Ordered from the furthest up to the directory itself.
	List<string> recurseDirectories(string directory)
	{
		List<string> directoryList = new List<string>();
		char[] separators = { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
		string[] pathParts = directory.Split(separators);
		for (int numParts = pathParts.Length; numParts > 0; numParts--)
		{
			string temp_path = pathParts[0];
			for (int i = 1; i < numParts; i++)
			{
				temp_path = Path.Combine(temp_path, pathParts[i]);
			}
			directoryList.Add(temp_path);
		}
		return directoryList;
	}


	/*
	* STRING TO FLOAT/VECTOR PARSING
	*/

	// Given a string, attempt to parse it into an array of floats.
	// Return success state, with output filled with the values and outputCount filled with the number of elements in the array.
	public static bool parseNumericValue(string input, float[] output, out int outputCount)
	{
		string[] splitStr = { ", " };
		string[] elements = input.Split(splitStr, System.StringSplitOptions.RemoveEmptyEntries);
		int eleCount = Mathf.Min(elements.Length, output.Length);
		bool success = false;
		for (int e = 0; e < eleCount; e++)
		{
			success = float.TryParse(elements[e], out output[e]);
			if (!success)
			{
				break;
			}
		}
		outputCount = (success) ? eleCount : 0;
		return success;
	}


	/*
	* TEXTURE ASSET FINDING AND FILE LOADING
	*/

	// Given a texture filename and a list of paths, attempt to find an existing Texture asset that matches.
	Texture findExistingTexture(List<string> pathsToAssetDirectory, string textureFile)
	{
		Texture tex = null;

		// Look in the Textures directory of each directory up to root Assets directory.
		foreach (string test_path in pathsToAssetDirectory)
		{
			string test_assetPath = NormalizePath(Path.Combine(Path.Combine(test_path, textureDir), textureFile));
			tex = AssetDatabase.LoadAssetAtPath(test_assetPath, typeof(Texture)) as Texture;
			if (tex != null)
			{
				return tex;
			}
		}

		// Otherwise find any texture with this name in the entire project.

		// AssetDatabase.FindAssets crashes here if this is a fresh import, so we need to do this manually until Unity fix it.
		// This is the FindAssets code path, leaving here for when the bug is fixed.
		/*
		string textureName = Path.GetFileNameWithoutExtension (textureFile);
		string[] guids = AssetDatabase.FindAssets ("\"" + textureName + "\"" + " t:texture");
		foreach (string guid in guids) {
			tex = AssetDatabase.LoadAssetAtPath (AssetDatabase.GUIDToAssetPath (guid), typeof(Texture)) as Texture;
			if (tex != null) {
				return tex;
			}
		}*/


		// This is the annoying long-winded manual search.
		string temp_path = Path.Combine("Assets", textureDir);
		tex = AssetDatabase.LoadAssetAtPath(NormalizePath(Path.Combine(temp_path, textureFile)), typeof(Texture)) as Texture;
		if (tex != null)
		{
			return tex;
		}
		foreach (string d in Directory.GetDirectories("Assets", "*", SearchOption.AllDirectories))
		{
			if (d.EndsWith(textureDir, StringComparison.CurrentCultureIgnoreCase))
			{
				continue;
			}
			temp_path = Path.Combine(d, textureDir);
			tex = AssetDatabase.LoadAssetAtPath(NormalizePath(Path.Combine(temp_path, textureFile)), typeof(Texture)) as Texture;
			if (tex != null)
			{
				return tex;
			}
		}

		// Otherwise return null.
		return null;
	}

	// Given a MODOMaterialPropertyTexture, asset directory paths, texture root path, find that texture or load in the external one defined in the property.
	Texture loadTextureFromProperty(MODOMaterialContainer matContainer, MODOMaterialPropertyTexture matProp, List<string> pathsToAssetDirectory, string textureRootPath, bool force, out string texturePath, out MODOColorspace colorspace)
	{
		string textureFilename = null;
		MODOColorspace textureColorspace = MODOColorspace.sRGB;

		if ((matProp.imageIndex > -1) && (matContainer.images.files.Count > matProp.imageIndex))
		{
			if (!string.IsNullOrEmpty(matContainer.images.files[matProp.imageIndex].file))
			{
				textureFilename = matContainer.images.files[matProp.imageIndex].file;
				textureColorspace = matContainer.images.files[matProp.imageIndex].colorspace;
			}
		}
		colorspace = textureColorspace;

		// Try and fall back to the XML version 0 filename.
		/*if (string.IsNullOrEmpty(textureFilename) && !string.IsNullOrEmpty(matProp.filename)) {
			textureFilename = matProp.filename;
		}*/

		if (!string.IsNullOrEmpty(textureFilename))
		{
			string textureFile = Path.GetFileName(textureFilename);
			string assetDirectory = pathsToAssetDirectory[Mathf.Max(pathsToAssetDirectory.Count - 1, 0)];

			// If the texture already exists (by Unity's search standards) use it.
			Texture tex = findExistingTexture(pathsToAssetDirectory, textureFile);
			if (!force && (tex != null))
			{
				texturePath = NormalizePath(AssetDatabase.GetAssetPath(tex));
				return tex;
			}

			// Otherwise load in the texture from the absolute path, if it exists.

			// External file path.
			string importPath = NormalizePath(Path.Combine(textureRootPath, textureFilename));

			// Check that the extnernal texture file exists before we do anything.
			if (File.Exists(importPath))
			{
				string importedDirectory = NormalizePath(Path.Combine(assetDirectory, "Textures"));
				// If the Textures directory doesn't exist, then create it.
				if (!Directory.Exists(importedDirectory) && !AssetDatabase.IsValidFolder(importedDirectory))
				{
					importedDirectory = NormalizePath(AssetDatabase.GUIDToAssetPath(AssetDatabase.CreateFolder(assetDirectory, "Textures")));
				}

				// Get the path of where the imported file will wind up, relative to the project directory.
				string importedFile = NormalizePath(Path.Combine(importedDirectory, textureFile));

				if (importExternalFile(importPath, importedFile))
				{
					AssetDatabase.ImportAsset(importedFile, ImportAssetOptions.ForceUpdate);
					texturePath = importedFile;
				}
				else
				{
					texturePath = null;
				}
				return null;
			}
		}
		texturePath = null;
		return null;
	}

	// Attempt to replace a file with another one.
	// But don't bother if they point to the same file.
	bool importExternalFile(string externalPath, string internalPath)
	{
		// Check we're not trying to overwrite a file with itself.
		if (NormalizePath(externalPath).Equals(NormalizePath(Path.Combine(projectPath, internalPath)), StringComparison.CurrentCultureIgnoreCase))
		{
			DebugLog("Trying to import " + externalPath + " but it's the same file as the target. Skipping.");
			return false;
		}
		else
		{
			FileUtil.ReplaceFile(externalPath, internalPath);
			return true;
		}
	}


	/*
	* MATERIAL ASSET FINDING
	*/

	// Figure out the target material name, based on the user's import preferences for the mesh asset.
	string getMaterialName(string modelName, ModelImporterMaterialName materialName, Material material)
	{
		if (materialName == ModelImporterMaterialName.BasedOnMaterialName)
		{
			return material.name;
		}
		else if (materialName == ModelImporterMaterialName.BasedOnModelNameAndMaterialName)
		{
			return (modelName + "-" + material.name);
		}
		else if (materialName == ModelImporterMaterialName.BasedOnTextureName)
		{
			Texture mainTex = material.GetTexture("_MainTex");
			if (mainTex != null)
			{
				return mainTex.name;
			}
			else
			{
				return material.name;
			}
		}
		return material.name;
	}

	// Check through the Assets directory to find if this material already exists.
	string getMaterialPath(List<string> pathsToAssetDirectory, string importedMaterialFile, string importedMaterialName, ModelImporterMaterialSearch materialSearch, out bool materialAlreadyExists)
	{
		// Default to not already existing.
		materialAlreadyExists = false;
		string path = pathsToAssetDirectory[Mathf.Max(pathsToAssetDirectory.Count - 1, 0)];

		if (materialSearch == ModelImporterMaterialSearch.Local)
		{
			// Look just in the local Materials directory.
			string temp_path = Path.Combine(path, materialDir);
			if (AssetDatabase.LoadAssetAtPath(Path.Combine(temp_path, importedMaterialFile), typeof(Material)))
			{
				materialAlreadyExists = true;
			}
			return temp_path;
		}
		else if (materialSearch == ModelImporterMaterialSearch.RecursiveUp)
		{
			// Look in the Materials directory of each directory up to root Assets directory.
			// Fall back to the local Materials directory if not found.
			foreach (string test_path in pathsToAssetDirectory)
			{
				string temp_path = Path.Combine(test_path, materialDir);
				if (AssetDatabase.LoadAssetAtPath(Path.Combine(temp_path, importedMaterialFile), typeof(Material)))
				{
					materialAlreadyExists = true;
					return temp_path;
				}
			}
		}
		else if (materialSearch == ModelImporterMaterialSearch.Everywhere)
		{
			// Look across the entire project for a material with this name, grab the first result.
			// Fall back to the local Materials directory if not found.
			string[] guids = AssetDatabase.FindAssets(importedMaterialName + " t:material", null);
			if (guids.Length > 0)
			{
				materialAlreadyExists = true;
				return AssetDatabase.GUIDToAssetPath(guids[0]);
			}
		}
		return Path.Combine(path, materialDir);
	}

	/*
	* MATERIAL KEYWORDS AND BLEND MODES
	*/

	// Set up the blend modes for the material.
	// Copied from StandardShaderGUI.cs.
	public enum BlendMode
	{
		Opaque,
		Cutout,
		Fade,       // Old school alpha-blending mode, fresnel does not affect amount of transparency
		Transparent // Physically plausible transparency mode, implemented as alpha pre-multiply
	}

	// Set up the blend modes for the material.
	// Copied from StandardShaderGUI.cs.
	void SetupMaterialWithBlendMode(Material material, BlendMode blendMode)
	{
		// Hack because SetOverrideTag wasn't introduced until Unity 5.1
		// but the version specific defines weren't introduced until 5.3
		MethodInfo overrideTagMethod = typeof(Material).GetMethod("SetOverrideTag");

		switch (blendMode)
		{
			case BlendMode.Opaque:
				if (overrideTagMethod != null)
					overrideTagMethod.Invoke(material, new object[] { "RenderType", "" });
				material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
				material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
				material.SetInt("_ZWrite", 1);
				material.DisableKeyword("_ALPHATEST_ON");
				material.DisableKeyword("_ALPHABLEND_ON");
				material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
				material.renderQueue = -1;
				break;
			case BlendMode.Cutout:
				if (overrideTagMethod != null)
					overrideTagMethod.Invoke(material, new object[] { "RenderType", "TransparentCutout" });
				material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
				material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
				material.SetInt("_ZWrite", 1);
				material.EnableKeyword("_ALPHATEST_ON");
				material.DisableKeyword("_ALPHABLEND_ON");
				material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
				material.renderQueue = 2450;
				break;
			case BlendMode.Fade:
				if (overrideTagMethod != null)
					overrideTagMethod.Invoke(material, new object[] { "RenderType", "Transparent" });
				material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
				material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
				material.SetInt("_ZWrite", 0);
				material.DisableKeyword("_ALPHATEST_ON");
				material.EnableKeyword("_ALPHABLEND_ON");
				material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
				material.renderQueue = 3000;
				break;
			case BlendMode.Transparent:
				if (overrideTagMethod != null)
					overrideTagMethod.Invoke(material, new object[] { "RenderType", "Transparent" });
				material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
				material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
				material.SetInt("_ZWrite", 0);
				material.DisableKeyword("_ALPHATEST_ON");
				material.DisableKeyword("_ALPHABLEND_ON");
				material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
				material.renderQueue = 3000;
				break;
		}
	}

	void SetupMaterialUseAlbedoAlpha(Material material, bool albedoAlpha)
	{
		if (isAtLeastUnity5_4)
		{
			if (albedoAlpha)
			{
				material.SetFloat("_SmoothnessTextureChannel", 1.0f);
				material.EnableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
			}
			else
			{
				material.SetFloat("_SmoothnessTextureChannel", 0.0f);
				material.DisableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
			}
		}
	}

	/*
	* MATERIAL CREATION AND TEXTURE ASSIGNMENT
	*/

	Material OnAssignMaterialModel(Material material, Renderer renderer)
	{
		if (!isAtLeastUnity5)
		{
			return null;
		}

		// Check to see if there's an XML file to match with the FBX asset.
		string xmlPath = NormalizePath(Path.Combine(Path.GetDirectoryName(assetPath), Path.GetFileNameWithoutExtension(assetPath) + ".xml"));
		MODOMaterialContainer matContainer = LoadMaterialXMLFromPath(xmlPath);

		bool pref_showWarnings = EditorPrefs.GetBool("MODOMaterialShowWarnings", true);

		// If no XML material exists, return null and Unity will default back to it's own default methods of material loading.
		if (matContainer == null)
		{
			DebugWarning("No XML file for material \"" + material.name + "\"found.");
			return null;
		}

		string modelPath = NormalizePath(Path.Combine(Path.GetDirectoryName(xmlPath), Path.GetFileNameWithoutExtension(xmlPath) + ".fbx"));
		if (AssetDatabase.LoadAssetAtPath(modelPath, typeof(GameObject)) == null)
		{
			DebugWarning("No FBX file for XML \"" + xmlPath + "\"found.");
		}

		// Here, the material XML file is valid and has been loaded.
		// Get the tags on the file - this stores the per-file import preferences, if any.
		bool xml_alwaysApply = EditorPrefs.GetBool("MODOMaterialAlwaysApply", true);
		bool xml_alwaysImport = EditorPrefs.GetBool("MODOMaterialAlwaysImport", false);
		TextAsset xmlAsset = AssetDatabase.LoadAssetAtPath(xmlPath, typeof(TextAsset)) as TextAsset;
		string[] labels = AssetDatabase.GetLabels(xmlAsset);
		if (labels.Length > 0)
		{
			foreach (string label in labels)
			{
				if (label.ToLowerInvariant() == "MODOAlwaysApply".ToLowerInvariant())
					xml_alwaysApply = true;
				if (label.ToLowerInvariant() == "MODOAlwaysImport".ToLowerInvariant())
					xml_alwaysImport = true;
				if (label.ToLowerInvariant() == "MODODontApply".ToLowerInvariant())
					xml_alwaysApply = false;
				if (label.ToLowerInvariant() == "MODODontImport".ToLowerInvariant())
					xml_alwaysImport = false;
			}
		}

		// First parse the paths to get some names and directories to search later.
		// Get the mesh asset directory and name.
		string assetDirectory = Path.GetDirectoryName(assetPath);
		DebugLog("Asset Directory: " + assetDirectory);

		string assetName = Path.GetFileNameWithoutExtension(assetPath);
		DebugLog("Asset Name: " + assetName);

		string textureRootPath = (matContainer.useRootPath == 1) ? matContainer.rootPath : Path.Combine(projectPath, assetDirectory);
		DebugLog("Texture Root Path: " + textureRootPath);

		// Get all the paths from here to root of Assets directory.
		List<string> pathsToAssetDirectory = recurseDirectories(assetDirectory);

		// Get the model's material search and name settings.
		ModelImporter modelImporter = assetImporter as ModelImporter;
		ModelImporterMaterialSearch materialSearch = modelImporter.materialSearch;
		ModelImporterMaterialName materialName = modelImporter.materialName;

		// Get the material name based on material name setting.
		string importedMaterialName = getMaterialName(assetName, materialName, material);

		// Path to the material file itself (with extension).
		string importedMaterialFile = importedMaterialName + ".mat";

		// The directory path (relative to the project root directory) of the material.
		// Figure out the material directory path based on the Mesh's material search setting.
		// The found variable is true if an existing material was found at that path. False if a material needs to be created.
		bool materialAlreadyExists = false;
		string importedMaterialPath = getMaterialPath(pathsToAssetDirectory, importedMaterialFile, importedMaterialName, materialSearch, out materialAlreadyExists);

		// The full path to the material file (relative to the project root directory).
		string materialPath = NormalizePath(Path.Combine(importedMaterialPath, importedMaterialFile));

		// Get the original name of the material it wants. We need this to get the material values from the XML.
		// The material itself may have different name due to the import settings.
		string matName = material.name;

		// If there's a material here already and we're not always updating the values, just return that material.
		if (AssetDatabase.LoadAssetAtPath(materialPath, typeof(Material)))
		{
			material = AssetDatabase.LoadAssetAtPath(materialPath, typeof(Material)) as Material;
			if (!xml_alwaysApply)
			{
				return material;
			}
		}

		// Apply the PBR shader to the material if it's not already applied.
		Shader pbrShader = Shader.Find(shaderName);
		if ((pbrShader != null) && ((material.shader == null) || (material.shader != pbrShader)))
		{
			material.shader = pbrShader;
		}
		// Check the shader has been properly applied, error out if not.
		if (material.shader == null)
		{
			DebugLog("Unable to find shader \"" + shaderName + "\".");
			return null;
		}

		// Get the material definition from the material container.
		MODOMaterial mat = matContainer.getMaterial(matName);
		if (mat == null)
		{
			DebugLog("XML file has no definition for material \"" + matName + "\".");
			return null;
		}

		// Set diffuse color of the material.
		// And if opacity is less than 1, then set the material blend mode to fade (z-primed transparent).
		Color diffuseColor = mat.getDiffuseColor();
		material.SetColor("_Color", diffuseColor);
		DebugLog("Setting " + mat.name + ": Color to " + diffuseColor);
		if (diffuseColor.a < 1.0f)
		{
			SetupMaterialWithBlendMode(material, BlendMode.Fade);
			DebugLog("Setting " + mat.name + ": to Fade.");
		}

		// Set emissive color & level of the material.
		Color emissiveColor = mat.getEmissiveColor();
		if (Mathf.Max(emissiveColor.r, emissiveColor.g, emissiveColor.b) > (0.1f / 255.0f))
		{
			material.EnableKeyword("_EMISSION");
		}
		material.SetColor("_EmissionColor", emissiveColor);
		DebugLog("Setting " + mat.name + ": Emission Color to " + emissiveColor);

		List<MODOTexture> finalTextures = new List<MODOTexture>();

		// Store any warnings for this material.
		List<string> warnings = new List<string>();
		List<string> debugWarnings = new List<string>();

		// For each material slot, load up the information for it.
		// If there is no texture defined, fall back to the scalar/vector value.
		// Any textures that need to be imported are added to a pending list to be applied after they've been imported.
		foreach (MaterialParameter parameter in materialParams)
		{
			MODOMaterialProperty matProp = mat.getProperty(parameter.name);
			if (matProp == null)
			{
				continue;
			}

			// Attempt to load in a texture for the slot.
			if (parameter.tSlot != null)
			{
				bool validTexture = false;

				MODOTexture finalTexture = new MODOTexture(material, null, null, parameter.tSlot, parameter.name, MODOChannel.RGB, MODOColorspace.sRGB);

				// Iterate through each of the textures listed for this slot (there might be multiple textures) and find the first valid one.
				foreach (MODOMaterialPropertyTexture matPropTexture in matProp.textures)
				{

					// Set up the wrap values for the main maps.
					if (finalTexture.name == "Albedo")
					{
						Vector2 scale = matPropTexture.getWrapUV(material.GetTextureScale(finalTexture.slot));
						DebugLog("Setting " + mat.name + ": " + finalTexture.name + " Wrap UV to (" + scale.x + ", " + scale.y + ").");
						material.SetTextureScale(finalTexture.slot, scale);
					}

					// Set up the wrap values for the detail maps.
					if (finalTexture.name == "Detail Albedo x2")
					{
						if (matPropTexture.uvmap != MODOUVMap.UV1)
						{
							material.SetFloat("_UVSec", 1.0f);
							DebugLog("Setting " + mat.name + ": Secondary UV to UV1");
						}
						else
						{
							material.SetFloat("_UVSec", 0.0f);
							DebugLog("Setting " + mat.name + ": Secondary UV to UV0");
						}
						Vector2 scale = matPropTexture.getWrapUV(material.GetTextureScale(finalTexture.slot));
						DebugLog("Setting " + mat.name + ": " + finalTexture.name + " Wrap UV to (" + scale.x + ", " + scale.y + ").");
						material.SetTextureScale(finalTexture.slot, scale);
					}

					finalTexture.texture = loadTextureFromProperty(matContainer, matPropTexture, pathsToAssetDirectory, textureRootPath, xml_alwaysImport, out finalTexture.path, out finalTexture.colorspace);
					if ((finalTexture.texture != null) || (finalTexture.path != null))
					{
						// We found a texture.
						validTexture = true;
						if (finalTexture.texture != null)
						{
							finalTexture.path = AssetDatabase.GetAssetPath(finalTexture.texture);
							DebugLog("Setting " + mat.name + ": " + matProp.name + " to " + finalTexture.texture.name);
						}
						else
						{
							if (pendingTextures.addPendingTexture(material, finalTexture))
							{
								DebugLog("Deferred setting " + mat.name + ": " + matProp.name + " to " + finalTexture.path);
								finalTexture.channel = matPropTexture.channel;
							}
							else
							{
								DebugLog(mat.name + ": " + matProp.name + " already has a texture waiting to be applied, skipping application of " + finalTexture.path);
							}
						}

						// Alert the user if their texture channels are mis-wired.
						if (finalTexture.channel != parameter.channel)
						{
							if (pref_showWarnings)
								warnings.Add(finalTexture.name + " texture is assigned to the wrong texture channel (currently " + finalTexture.channel + ", Unity wants it in " + parameter.channel + ").");
							debugWarnings.Add(mat.name + "'s " + finalTexture.name + " value is being driven by the " + finalTexture.channel + " channel of the " + finalTexture.name + " texture.\nUnity expects it to be in the " + parameter.channel + " channel.");
						}

						break;
					}
				}

				if (!validTexture)
				{
					finalTexture.texture = null;
					finalTexture.path = null;
					material.SetTexture(finalTexture.slot, null);
					DebugLog("Clearing texture for " + mat.name + ": " + finalTexture.name + " (" + finalTexture.slot + ")");
				}

				finalTextures.Add (finalTexture);
			}

			// Fill the scalar value slot, if it has one.
			if (parameter.sSlot != null)
			{
				if (matProp.vectorCount == 1)
				{
					// Scalar channel.
					DebugLog("Setting " + mat.name + ": " + matProp.name + " to " + matProp.vector[0]);
					material.SetFloat(parameter.sSlot, matProp.vector[0]);
				}
				else if (matProp.vectorCount > 1)
				{
					// Vector channel.
					material.SetVector(parameter.sSlot, matProp.vector);
					DebugLog("Setting " + mat.name + ": " + matProp.name + " to " + matProp.vector);
				}
			}
		}

		// Check to see if there are any conflicts with the Metallic/Albedo/Smoothness maps.
		// Users might assign them arbitrarily in MODO, but Unity is much more fussy and wants the Smoothness to be derived from specific texture channels.
		// In Unity 5.0, the Smoothness value is derived from the Alpha channel of the texture used for the Metallic value.
		// As of Unity 5.4, the Smoothness value can optionally be derived from the Albedo texture's alpha channel instead.
		{
			// Check to see if the material already has either Metallic or Albedo texture applied and which texture it's deriving the smoothness from.
			bool wantsAlbedoAlpha = (isAtLeastUnity5_4 && material.IsKeywordEnabled("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A"));

			MODOTexture metalTex = null;
			MODOTexture albedoTex = null;
			MODOTexture smoothTex = null;

			// Find all the pending textures.
			foreach (MODOTexture finalTexture in finalTextures)
			{
				if (finalTexture.name == "Metallic")
					metalTex = finalTexture;

				if (finalTexture.name == "Albedo")
					albedoTex = finalTexture;

				if (finalTexture.name == "Smoothness")
					smoothTex = finalTexture;
			}

			bool noMetalTex = ((metalTex == null) || string.IsNullOrEmpty(metalTex.path));
			bool noAlbedoTex = ((albedoTex == null) || string.IsNullOrEmpty(albedoTex.path));
			bool noSmoothTex = ((smoothTex == null) || string.IsNullOrEmpty(smoothTex.path));

			bool noPreviousMetalTexture = noMetalTex ? true : (metalTex.texture == null);
			//bool noPreviousAlbedoTexture = noAlbedoTex ? true : (albedoTex.texture == null);

			if (noSmoothTex)
			{
				// No smoothness map.
				if (noMetalTex && noAlbedoTex)
				{
					// No Metallic or albedo textures either. No problem.
				}
				else if (wantsAlbedoAlpha) {
					// The material wants the Albedo map Alpha as Smoothness.
					if (!noAlbedoTex)
					{
						// There is an Albedo map. Warn the user it's going to look wrong.
						if (pref_showWarnings)
							warnings.Add("Smoothness will be taken from the Albedo texture's alpha channel.");
						debugWarnings.Add("There is no Smoothness texture specified for " + mat.name + ".\nUnity is expecting to read the Smoothness value from the alpha channel of the Albedo texture.");
					}
					//else
					//{
					// No Albedo map anyway. No problem.
					//}
				}
				else {
					// The material wants the Metallic map Alpha as Smoothness.
					if (!noMetalTex)
					{
						if (noPreviousMetalTexture)
						{
							// There is a Metallic map. Warn the user it's going to look wrong.
							if (pref_showWarnings)
								warnings.Add("Metallic value will be taken from the Smoothness texture's alpha channel.");
							debugWarnings.Add("There is no Metallic texture specified for " + mat.name + ".\nUnity is expecting to read the Metallic value from the red channel of the Smoothness texture.");
						}
						else
						{
							// There is a Metallic map. Warn the user it's going to look wrong.
							if (pref_showWarnings)
								warnings.Add("Smoothness value will be taken from the Metallic texture's alpha channel.");
							debugWarnings.Add("There is no Smoothness texture specified for " + mat.name + ".\nUnity is expecting to read the Smoothness value from the alpha channel of the Metallic texture.");
						}
					}
					//else
					//{
					// No Metallic map anyway. No problem.
					//}
				}
			}
			else
			{
				// There is a Smoothness map.
				bool smoothUsesMetal = smoothTex.path.Equals(metalTex.path, StringComparison.CurrentCultureIgnoreCase);
				bool smoothUsesAlbedo = smoothTex.path.Equals(albedoTex.path, StringComparison.CurrentCultureIgnoreCase);

				if (!smoothUsesMetal && !smoothUsesAlbedo)
				{
					// Smoothness map isn't driven by either Metallic or Albedo.

					if (noMetalTex)
					{
						// There is no Metallic map. Warn the user it's going to look wrong.
						if (pref_showWarnings)
							warnings.Add("Metallic value will be taken from the Smoothness texture's red channel.");

						debugWarnings.Add("There is no Metallic texture specified for " + mat.name + ".\nUnity is expecting to read the Metallic value from the red channel of the Smoothness texture.");
					}
					else
					{
						// There is a Metallic map. Warn the user it's going to look wrong.
						if (pref_showWarnings)
						{
							if (isAtLeastUnity5_4)
								warnings.Add("Smoothness value is not driven by the correct texture, must be from Metallic or Albedo texture.");
							else
								warnings.Add("Smoothness value is not driven by the correct texture, must be from Metallic texture.");
						}

						if (isAtLeastUnity5_4)
							debugWarnings.Add("There is no Smoothness texture specified for " + mat.name + ".\nUnity is expecting to read the Smoothness value from the alpha channel of either the Albedo or the Metallic texture.");
						else
							debugWarnings.Add("There is no Smoothness texture specified for " + mat.name + ".\nUnity is expecting to read the Smoothness value from the alpha channel of the Metallic texture.");
					}
				}
				else if (smoothUsesMetal)
				{
					// Smoothness uses Metallic.
					if (wantsAlbedoAlpha)
					{
						// Set the shader to use that if Unity version supports that.
						SetupMaterialUseAlbedoAlpha(material, false);
						DebugLog("Setting " + mat.name + ": Take Smoothness value from Metallic texture alpha channel.");
					}

					// Smoothness is already using another texture, so clear it.
					smoothTex.texture = null;
					smoothTex.path = null;
				}
				else if (smoothUsesAlbedo && isAtLeastUnity5_4)
				{
					if (!wantsAlbedoAlpha)
					{
						// Smoothness uses Albedo and Unity version supports that.
						SetupMaterialUseAlbedoAlpha(material, true);
						DebugLog("Setting " + mat.name + ": Take Smoothness value from Albedo texture alpha channel.");
					}

					// Smoothness is already using another texture, so clear it.
					smoothTex.texture = null;
					smoothTex.path = null;
				}
				else
				{
					// Smoothness doesn't use Metallic and Unity version doesn't support anything else. Warn the user it's going to look wrong.
					if (pref_showWarnings)
						warnings.Add("Smoothness value is not driven by the correct texture.");
					if (isAtLeastUnity5_4)
						debugWarnings.Add("There is no Smoothness texture specified for " + mat.name + ".\nUnity is expecting to read the Smoothness value from either the alpha channel of the Albedo or the Metallic texture.");
					else
						debugWarnings.Add("There is no Smoothness texture specified for " + mat.name + ".\nUnity is expecting to read the Smoothness value from the alpha channel of the Metallic texture.");
				}
			}
		}

		// Assign any existing textures now.
		foreach (MODOTexture finalTexture in finalTextures)
		{
			if (finalTexture.texture != null)
			{
				material.SetTexture(finalTexture.slot, finalTexture.texture);
			}
		}

		// Create the directory for the material - and the material asset itself - if it doesn't already exist.
		if (!materialAlreadyExists)
		{
			if (!Directory.Exists(importedMaterialPath) && !AssetDatabase.IsValidFolder(importedMaterialPath))
			{
				AssetDatabase.CreateFolder(assetDirectory, materialDir);
			}
			// Create the material itself.
			AssetDatabase.CreateAsset(material, materialPath);
		}


		// Display any warnings to the user.
		if (pref_showWarnings && (warnings.Count > 0))
		{
			EditorUtility.DisplayDialog(
			"Material Issues Detected with \"" + mat.name + "\"",
			string.Join("\n\n", warnings.ToArray()) + "\n\nMaterials may not appear as expected!\n\nSee warning in Console for more details.",
			"OK",
			"");
			warnings.Clear();
		}

		// Write full details out to the console.
		// Display any warnings to the user.
		if (debugWarnings.Count > 0)
		{
			Debug.LogWarning("Material Issues Detected with \"" + mat.name + "\"\n\n" + string.Join("\n\n", debugWarnings.ToArray()) + "\n\n");
			debugWarnings.Clear();
		}

		// Return the material.
		return material;
	}

	// Handle any textures that weren't already imported at material creation time.
	// Here we're just ensuring that textures going into normal map slots are set as normal maps.
	void OnPreprocessTexture()
	{
		if (!isAtLeastUnity5)
		{
			return;
		}

		// Get all the textures at the path of this one.
		// Set them to be normal maps if they're meant to be.
		// Set up their colorspace.
		foreach (MODOMaterialPendingTexture pendingTexture in pendingTextures.getPendingTextures(assetPath))
		{
			TextureImporter textureImporter = assetImporter as TextureImporter;
			if ((pendingTexture.texture.slot == "_BumpMap") || (pendingTexture.texture.slot == "_DetailNormalMap"))
			{
				if (textureImporter.textureType != TextureImporterType.Bump)
				{
					textureImporter.textureType = TextureImporterType.Bump;
				}
			}
			else
			{
				if (pendingTexture.texture.colorspace == MODOColorspace.sRGB)
				{
					textureImporter.textureType = TextureImporterType.Image;
				}
				else if (pendingTexture.texture.colorspace == MODOColorspace.linear)
				{
					textureImporter.textureType = TextureImporterType.Advanced;
					textureImporter.linearTexture = true;
				}
			}
		}
	}

	// Handle any textures that weren't already imported at material creation time.
	void OnPostprocessTexture(Texture texture)
	{
		if (!isAtLeastUnity5)
		{
			return;
		}
		// Get all the textures at the path of this one.
		// And tell them they're ready to be applied to their materials.
		foreach (MODOMaterialPendingTexture pendingTexture in pendingTextures.getPendingTextures(assetPath))
		{
			pendingTexture.state = MODOPendingState.ReadyToApply;
		}
		// The material will have the slot filled after loading has finished.
		EditorApplication.delayCall += pendingTextures.applyTexture;
	}


	/*
	* POSTPROCESSOR
	*/

	static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
	{
		if (!isAtLeastUnity5)
		{
			return;
		}

		pref_showWarnings = EditorPrefs.GetBool("MODOMaterialShowWarnings", true);

		// Check to see if we've just imported a MODO Material XML file.
		// If we have, and we haven't also just imported it's respective FBX file, then re-import the FBX file.
		// That will force the AssetImporter to update the material values to whatever's been changed in this XML file.
		foreach (var str in importedAssets)
		{
			if (Path.GetExtension(str) == ".xml")
			{
				MODOMaterialContainer matContainer = LoadMaterialXMLFromPath(str);
				if (matContainer != null)
				{
					string modelPath = Path.Combine(Path.GetDirectoryName(str), Path.GetFileNameWithoutExtension(str) + ".fbx");
					if (importedAssets.Contains(modelPath))
					{
						DebugLog("FBX was just (re)imported with this XML file, so not (re)importing it.");
					}
					else
					{
						if (AssetDatabase.LoadAssetAtPath(modelPath, typeof(GameObject)) != null)
						{
							AssetDatabase.ImportAsset(modelPath, ImportAssetOptions.ForceUpdate);
							DebugLog("Reimporting FBX file for XML.");
						}
						else
						{
							DebugWarning("MODO Material XML file imported without corresponding FBX mesh.\n" + modelPath + " is missing.");
							if (pref_showWarnings)
							{
								EditorUtility.DisplayDialog(
								"Missing FBX for Material",
								"MODO Material XML file imported without corresponding FBX mesh.\n\"" + Path.Combine(Directory.GetCurrentDirectory(), modelPath) + "\" is missing.",
								"OK",
								"");
							}
						}
					}
				}
			}
		}
	}
#endif

	// Add default preferences to the Preferences Window.
	[PreferenceItem("MODO Importer")]
	public static void PreferencesGUI()
	{
		// Load the preferences
		pref_alwaysApply = EditorPrefs.GetBool("MODOMaterialAlwaysApply", true);
		pref_alwaysImport = EditorPrefs.GetBool("MODOMaterialAlwaysImport", false);
		pref_showWarnings = EditorPrefs.GetBool("MODOMaterialShowWarnings", true);
		pref_debug = EditorPrefs.GetBool("MODOMaterialDebug", false);

		// Preferences GUI
		EditorGUILayout.LabelField("Default Settings", EditorStyles.boldLabel);
		pref_alwaysApply = EditorGUILayout.Toggle("Force Update Materials", pref_alwaysApply);
		EditorGUI.BeginDisabledGroup(pref_alwaysApply == false);
		pref_alwaysImport = EditorGUILayout.Toggle("Force Texture Import", pref_alwaysImport);
		EditorGUI.EndDisabledGroup();
		pref_showWarnings = EditorGUILayout.Toggle("Show Importer Warnings", pref_showWarnings);
		pref_debug = EditorGUILayout.Toggle("Debug", pref_debug);

		// Save the preferences
		if (GUI.changed)
		{
			EditorPrefs.SetBool("MODOMaterialAlwaysApply", pref_alwaysApply);
			EditorPrefs.SetBool("MODOMaterialAlwaysImport", pref_alwaysImport);
			EditorPrefs.SetBool("MODOMaterialDebug", pref_debug);
			EditorPrefs.SetBool("MODOMaterialShowWarnings", pref_showWarnings);
		}
	}

	// Make a nicer Inspector for the MODO Material XML file.
	// Default to the Default Inspector if it's not a MODO Material XML file.
	[CustomEditor(typeof(TextAsset))]
	public class TextAssetEditor : Editor
	{

#if MODO_COMPATIBLE_UNITY_VERSION
		bool showVersion = false;
		List<bool> toggles = new List<bool>();
		int optionApplyIndex = 0;
		int optionImportIndex = 0;
#endif

		public void showContent(TextAsset textAsset)
		{
			EditorGUILayout.TextArea(textAsset.text, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
		}

		public override void OnInspectorGUI()
		{

			TextAsset textAsset = target as TextAsset;
			MODOMaterialContainer matContainer = LoadMaterialXMLFromString(textAsset.text);
			if (matContainer == null)
			{
				showContent(textAsset);
			}
			else
			{
#if MODO_COMPATIBLE_UNITY_VERSION
				// Enable the GUI so you can toggle bits and pieces.
				GUI.enabled = true;

				// Get the tags on the file - this stores the per-file import preferences, if any.
				optionApplyIndex = 0;
				optionImportIndex = 0;
				bool pref_alwaysApply = EditorPrefs.GetBool("MODOMaterialAlwaysApply", true);
				bool pref_alwaysImport = EditorPrefs.GetBool("MODOMaterialAlwaysImport", false);
				string[] labels = AssetDatabase.GetLabels(textAsset);
				if (labels.Length > 0)
				{
					foreach (string label in labels)
					{
						if (label.ToLowerInvariant() == "MODOAlwaysApply".ToLowerInvariant())
						{
							optionApplyIndex = 1;
						}
						else if (label.ToLowerInvariant() == "MODODontApply".ToLowerInvariant())
						{
							optionApplyIndex = 2;
						}
						else if (label.ToLowerInvariant() == "MODOAlwaysImport".ToLowerInvariant())
						{
							optionImportIndex = 1;
						}
						else if (label.ToLowerInvariant() == "MODODontImport".ToLowerInvariant())
						{
							optionImportIndex = 2;
						}
					}
				}

				string[] applyOptions = new string[] { "Default", "Always Update", "Don't Update" };
				applyOptions[0] += " (" + applyOptions[(pref_alwaysApply) ? 1 : 2] + ")";
				string[] importOptions = new string[] { "Default", "Always Import", "Don't Import" };
				importOptions[0] += " (" + importOptions[(pref_alwaysImport) ? 1 : 2] + ")";

				EditorGUILayout.LabelField("MODO Material Importer Settings", EditorStyles.boldLabel);
				optionApplyIndex = EditorGUILayout.Popup("Material Import", optionApplyIndex, applyOptions);
				optionImportIndex = EditorGUILayout.Popup("Texture Import", optionImportIndex, importOptions);

				if (GUILayout.Button("Set As Defaults"))
				{
					if (optionApplyIndex > 0)
					{
						EditorPrefs.SetBool("MODOMaterialAlwaysApply", (optionApplyIndex == 1));
						optionApplyIndex = 0;
					}
					if (optionImportIndex > 0)
					{
						EditorPrefs.SetBool("MODOMaterialAlwaysImport", (optionImportIndex == 1));
						optionImportIndex = 0;
					}
					GUI.changed = true;
				}

				if (GUILayout.Button("Reimport"))
				{
					string assetPath = AssetDatabase.GetAssetPath(target);
					AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
				}

				EditorGUILayout.Separator();

				EditorGUILayout.LabelField("MODO Material File Contents", EditorStyles.boldLabel);

				if (matContainer.useRootPath == 1)
				{
					EditorGUILayout.LabelField("Root Texture Path: " + matContainer.rootPath);
				}

				for (int i = toggles.Count; i < matContainer.materials.Count; i++) { toggles.Add(false); }
				int toggleIdx = 0;
				foreach (MODOMaterial mat in matContainer.materials)
				{
					toggles[toggleIdx] = EditorGUILayout.Foldout(toggles[toggleIdx], mat.name);
					if (toggles[toggleIdx])
					{
						foreach (MaterialParameter param in materialParams)
						{
							MODOMaterialProperty matProp = mat.getProperty(param.name);
							if (matProp != null)
							{
								if (
									((matProp.numTextures() > 0) && (param.tSlot != null))
									||
									((matProp.value != null) && (param.sSlot != null))
									)
								{

									EditorGUILayout.BeginVertical();
									EditorGUILayout.LabelField(matProp.name, EditorStyles.boldLabel);

									EditorGUI.indentLevel++;

									if (param.name == "Albedo")
									{
										EditorGUI.BeginDisabledGroup(true);
#if UNITY_5_3_OR_NEWER
										EditorGUILayout.ColorField(new GUIContent("Albedo Color"), mat.getDiffuseColor(), false, false, false, null);
#else
										EditorGUILayout.ColorField(new GUIContent("Albedo Color"), mat.getDiffuseColor());
#endif
										EditorGUI.EndDisabledGroup();
									}
									else if (param.name == "Emission")
									{
										EditorGUI.BeginDisabledGroup(true);
#if UNITY_5_3_OR_NEWER
										EditorGUILayout.ColorField(new GUIContent("Emissive Color"), mat.getEmissiveColor(), false, false, true, new ColorPickerHDRConfig(0.0f, 99.0f, 0.0f, 99.0f));
#else
										EditorGUILayout.ColorField(new GUIContent("Albedo Color"), mat.getDiffuseColor());
#endif
										EditorGUI.EndDisabledGroup();
									}

									if ((param.tSlot != null) && (matProp.numTextures() > 0))
									{
										foreach (MODOMaterialPropertyTexture matPropTexture in matProp.textures)
										{
											EditorGUILayout.BeginVertical();
											EditorGUILayout.LabelField("Texture Name", matPropTexture.name);
											string textureFilename = null;
											string textureFilepath = null;
											MODOColorspace textureColorspace = MODOColorspace.sRGB;
											if ((matPropTexture.imageIndex > -1) && (matPropTexture.imageIndex < matContainer.images.files.Count))
											{
												textureFilepath = matContainer.images.files[matPropTexture.imageIndex].file;
												textureColorspace = matContainer.images.files[matPropTexture.imageIndex].colorspace;
											}
											else if (!string.IsNullOrEmpty(matPropTexture.filename))
											{
												textureFilepath = matPropTexture.filename;
											}
											if (!string.IsNullOrEmpty(textureFilepath))
											{
												textureFilename = Path.GetFileName(textureFilepath);
												EditorGUILayout.LabelField("Texture", textureFilename);

												if (textureColorspace == MODOColorspace.sRGB)
													EditorGUILayout.LabelField("Colorspace", "sRGB");
												else
													EditorGUILayout.LabelField("Colorspace", "Linear");

												if ((matPropTexture.wrapU != 1.0f) || (matPropTexture.wrapV != 1.0f))
													EditorGUILayout.LabelField("Tiling", matPropTexture.wrapU + ", " + matPropTexture.wrapV);

												if (matPropTexture.uvmap != MODOUVMap.UV1)
													EditorGUILayout.LabelField("Secondary UV Map");
											}
											EditorGUILayout.EndVertical();
										}
									}
									if ((param.sSlot != null) && (matProp.vectorCount > 0))
									{
										if (matProp.vectorCount > 1)
										{
											EditorGUI.BeginDisabledGroup(true);
											EditorGUILayout.ColorField(matProp.vector);
											EditorGUI.EndDisabledGroup();
										}
										else
										{
											EditorGUILayout.LabelField("Value", matProp.value);
										}
									}
									EditorGUI.indentLevel--;
									EditorGUILayout.EndVertical();
								}
							}
						}
					}
					toggleIdx++;
				}

				EditorGUILayout.Separator();
				pref_debug = EditorGUILayout.Toggle("Show Debug Info", pref_debug);
				if (pref_debug)
				{
					EditorGUILayout.Separator();
					showVersion = EditorGUILayout.Foldout(showVersion, "Version Info");
					if (showVersion)
					{
						EditorGUILayout.BeginVertical();
						if (matContainer.MODOversion != null)
						{
							EditorGUILayout.LabelField("App: " + matContainer.MODOversion.app);
							EditorGUILayout.LabelField("Version: " + matContainer.MODOversion.version);
							EditorGUILayout.LabelField("Build: " + matContainer.MODOversion.build);
							EditorGUILayout.LabelField("XML Version: " + matContainer.MODOversion.versionXML);
						}
						EditorGUILayout.EndVertical();
					}
				}

				if (GUI.changed)
				{
					// Get the tags on the file - this stores the per-file import preferences, if any.
					List<string> newLabels = new List<string>();
					if (labels.Length > 0)
					{
						foreach (string label in labels)
						{
							if (label.ToLowerInvariant() == "MODOAlwaysApply".ToLowerInvariant())
								continue;
							if (label.ToLowerInvariant() == "MODOAlwaysImport".ToLowerInvariant())
								continue;
							if (label.ToLowerInvariant() == "MODODontApply".ToLowerInvariant())
								continue;
							if (label.ToLowerInvariant() == "MODODontImport".ToLowerInvariant())
								continue;
							newLabels.Add(label);
						}
					}

					if (optionApplyIndex == 1)
						newLabels.Add("MODOAlwaysApply");
					else if (optionApplyIndex == 2)
						newLabels.Add("MODODontApply");
					if (optionImportIndex == 1)
						newLabels.Add("MODOAlwaysImport");
					else if (optionImportIndex == 2)
						newLabels.Add("MODODontImport");

					AssetDatabase.SetLabels(textAsset, newLabels.ToArray<string>());

					EditorPrefs.SetBool("MODOMaterialDebug", pref_debug);
				}
#else
										EditorGUILayout.LabelField ("MODO Material Importer is for Unity 5+ Only", EditorStyles.boldLabel);
#endif
			}
		}
	}
}
