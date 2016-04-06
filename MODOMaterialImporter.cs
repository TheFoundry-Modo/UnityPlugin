#if UNITY_5
#define MODO_COMPATIBLE_UNITY_VERSION
#else
#undef MODO_COMPATIBLE_UNITY_VERSION
#endif

using UnityEditor;
using UnityEngine;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Xml.Serialization;


#if MODO_COMPATIBLE_UNITY_VERSION
/*
* PENDING TEXTURE CLASSES
*/

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
	public Material			material;
	public string			texturePath;
	public string			textureSlot;
	public MODOPendingState	state = MODOPendingState.Pending;
}

/* Holds the pending textures - textures that have been copied
 * into the texture directory at the time of material creation.
 * These get applied after they've been imported.
 */
public class MODOMaterialPendingTextureContainer
{
	// All the pending textures.
	List<MODOMaterialPendingTexture> pending = new List<MODOMaterialPendingTexture> ();

	// Predicate to find pending textures that are ready to apply.
	private static bool isReadyToApply (MODOMaterialPendingTexture obj) {
		return (obj.state == MODOPendingState.ReadyToApply);
	}

	// Predicate to find pending textures that were applied, successful or otherwise.
	private static bool applied (MODOMaterialPendingTexture obj) {
		return (obj.state == MODOPendingState.Applied);
	}

	// Add a new pending texture.
	// Only if it hasn't been added already.
	// Force may be set to true to overwrite an existing pending texture if it's already added.
	public bool addPendingTexture (Material material, string texturePath, string textureSlot, bool force=false) {
		int index = pending.FindIndex(pendingTexture => string.Compare(pendingTexture.textureSlot, textureSlot) == 0 && pendingTexture.material == material);
		if (index == -1) {
			MODOMaterialPendingTexture newPending = new MODOMaterialPendingTexture();
			newPending.material = material;
			newPending.texturePath = texturePath;
			newPending.textureSlot = textureSlot;
			pending.Add(newPending);
			return true;
		}
		else if (force) {
			pending[index].texturePath = texturePath;
			return true;
		}
		return false;
	}

	// Get all the pending textures with this texture path.
	public List<MODOMaterialPendingTexture> getPendingTextures (string texturePath) {
		return pending.FindAll (pendingTexture => string.Compare(pendingTexture.texturePath,texturePath, true) == 0);
	}

	// Get all the pending textures with this material.
	public List<MODOMaterialPendingTexture> getPendingTextures (Material material) {
		return pending.FindAll(pendingTexture => pendingTexture.material == material);
	}

	// Apply all the newly imported textures to their materials.
	public void applyTexture () {
		foreach (MODOMaterialPendingTexture pendingTexture in pending.FindAll(isReadyToApply)) {
			if (pendingTexture.material != null) {
				Texture texture = AssetDatabase.LoadAssetAtPath (pendingTexture.texturePath, typeof (Texture)) as Texture;
				if (texture != null) {
					pendingTexture.material.SetTexture (pendingTexture.textureSlot, texture);
					EditorUtility.SetDirty (pendingTexture.material);

					// If this is getting applied as a normal map, enable normal mapping.
					if ((pendingTexture.textureSlot == "_BumpMap") || (pendingTexture.textureSlot == "_DetailNormalMap")) {
						pendingTexture.material.EnableKeyword ("_NORMALMAP");
					}

					// If this is getting applied as a parallax map, enable parallax mapping.
					if (pendingTexture.textureSlot == "_ParallaxMap") {
						pendingTexture.material.EnableKeyword ("_PARALLAXMAP");
					}

					// If this is getting applied as a parallax map, enable parallax mapping.
					if (pendingTexture.textureSlot == "_ParallaxMap") {
						pendingTexture.material.EnableKeyword ("_PARALLAXMAP");
					}

					// If this is getting applied as a specular map, enable specular workflow.
					if (pendingTexture.textureSlot == "_SpecGlossMap") {
						pendingTexture.material.EnableKeyword ("_SPECGLOSSMAP");
					}

					// If this is getting applied as a metallic map, enable metallic workflow.
					if (pendingTexture.textureSlot == "_MetallicGlossMap") {
						pendingTexture.material.EnableKeyword ("_METALLICGLOSSMAP");
					}

					// If this is getting applied as a detail map, enable detail mapping.
					if ((pendingTexture.textureSlot == "_DetailAlbedoMap") || (pendingTexture.textureSlot == "_DetailNormalMap")) {
						pendingTexture.material.EnableKeyword ("_DETAIL_MULX2");
					}
				}
			}
			pendingTexture.state = MODOPendingState.Applied;
		}

		pending.RemoveAll (applied);

		if (pending.Count == 0) {
			AssetDatabase.Refresh ();
		}
	}
}
#endif


/*
* XML SPECIFICATION
*/


// Holds the information about a material property's texture.
// Loaded from XML.
public class MODOMaterialPropertyTexture {
	[XmlAttribute("name")]
	public string name;

	[XmlAttribute("filename")]
	public string filename;

	[XmlAttribute("channel")]
	public string channel;

	[XmlAttribute("wrapU")]
	public string wrapU;

	[XmlAttribute("wrapV")]
	public string wrapV;

	[XmlAttribute("uvmap")]
	public string uvmap;

	#if MODO_COMPATIBLE_UNITY_VERSION
	public Vector2 getWrapUV (Vector2 inScale) {
		float[] floatValues = new float[4];
		int floatCount = 0;
		Vector2 scale = new Vector2 (inScale.x, inScale.y);
		if (wrapU != null) {
			MODOMaterialImporter.parseNumericValue (wrapU, floatValues, out floatCount);
			if (floatCount == 1) {
				scale = new Vector2(floatValues[0], scale.y);
			}
		}
		if (wrapV != null) {
			MODOMaterialImporter.parseNumericValue (wrapV, floatValues, out floatCount);
			if (floatCount == 1) {
				scale = new Vector2(scale.x, floatValues[0]);
			}
		}
		return scale;
	}
	#endif
}

// Holds the information about a material's property (values and a list of textures, if any).
// Loaded from XML.
public class MODOMaterialProperty
{
	[XmlAttribute ("name")]
	public string name;

	[XmlAttribute ("value")]
	public string value;

	[XmlElement("texture")]
	public List<MODOMaterialPropertyTexture> textures = new List<MODOMaterialPropertyTexture>();

	#if MODO_COMPATIBLE_UNITY_VERSION
	public MODOMaterialPropertyTexture getTexture(string filename) {
		return textures.Find(property => property.filename == filename);
	}

	public int numTextures() {
		return textures.Count();
	}
	#endif
}

// Holds the information about a material as well as it's properties.
// Loaded from XML.
public class MODOMaterial
{
	[XmlAttribute ("ID")]
	public string ID;

	[XmlAttribute ("ptag")]
	public string name;

	[XmlAttribute ("type")]
	public string type;

	[XmlElement ("property")]
	public List<MODOMaterialProperty> properties = new List<MODOMaterialProperty> ();


	#if MODO_COMPATIBLE_UNITY_VERSION
	public MODOMaterialProperty getProperty (string name) {
		return properties.Find (property => property.name == name);
	}

	// Return the emissive color.
	public Color getDiffuseColor () {
		Color diffuseColor = Color.white;

		// If there's an albedo texture, then default to white.
		// Otherwise use the albedo color, if it's there.
		MODOMaterialProperty diffuseColorProperty = properties.Find (property => property.name == "Albedo");
		if (diffuseColorProperty != null) {
			if (diffuseColorProperty.value != null) {
				float[] diffuseColorValues = new float[4];
				int diffuseColorCount = 0;
				MODOMaterialImporter.parseNumericValue (diffuseColorProperty.value, diffuseColorValues, out diffuseColorCount);
				if (diffuseColorCount == 3) {
					diffuseColor.r = diffuseColorValues [0];
					diffuseColor.g = diffuseColorValues [1];
					diffuseColor.b = diffuseColorValues [2];
				}
			}
		}

		// Stick the opacity level into the alpha channel.
		MODOMaterialProperty opacityProperty = properties.Find (property => property.name == "Opacity");
		if (opacityProperty != null) {
			if (opacityProperty.value != null) {
				float[] opacityLevelValues = new float[4];
				int opacityLevelCount = 0;
				MODOMaterialImporter.parseNumericValue (opacityProperty.value, opacityLevelValues, out opacityLevelCount);
				if (opacityLevelCount == 1) {
					diffuseColor.a = opacityLevelValues [0];
				}
			}
		}

		return diffuseColor;
	}

	// Return the emissive color.
	public Color getEmissiveColor () {
		// Get the color.
		Color emissiveColor = Color.black;

		// If there's an emissive texture, then default to white.
		// Otherwise use the emissive color, if it's there.
		MODOMaterialProperty emissiveColorProperty = properties.Find (property => property.name == "Emission");
		if (emissiveColorProperty != null) {
			if (emissiveColorProperty.value != null) {
				float[] emissiveColorValues = new float[4];
				int emissiveColorCount = 0;
				MODOMaterialImporter.parseNumericValue (emissiveColorProperty.value, emissiveColorValues, out emissiveColorCount);
				if (emissiveColorCount == 3) {
					emissiveColor [0] = emissiveColorValues [0];
					emissiveColor [1] = emissiveColorValues [1];
					emissiveColor [2] = emissiveColorValues [2];
				}
			}
		}

		// Multiply the emissive color by the emissive level.
		MODOMaterialProperty emissiveLevelProperty = properties.Find (property => property.name == "Emissive Level");
		if (emissiveLevelProperty != null) {
			if (emissiveLevelProperty.value != null) {
				float[] emissiveLevelValues = new float[4];
				int emissiveLevelCount = 0;
				MODOMaterialImporter.parseNumericValue (emissiveLevelProperty.value, emissiveLevelValues, out emissiveLevelCount);
				if (emissiveLevelCount == 1) {
					// emissiveColor [0] *= emissiveLevelValues [0];
					// emissiveColor [1] *= emissiveLevelValues [0];
					// emissiveColor [2] *= emissiveLevelValues [0];
					emissiveColor = emissiveColor * emissiveLevelValues [0];
				}
			}
		}

		return emissiveColor;
	}
	#endif
}

// Holds the information about the version of MODO and exporter this XML came from.
// Loaded from XML.
public class MODOMaterialVersion {
	[XmlAttribute("app")]
	public string app;

	[XmlAttribute("build")]
	public int build;

	[XmlText]
	public int version;
}

// Holds the materials in this asset.
// Loaded from XML.
[XmlRoot("catalog")]
public class MODOMaterialContainer
{
	[XmlElement("useRootPath")]
	public int useRootPath;

	[XmlElement("RootPath")]
	public string rootPath;

	[XmlElement("Version")]
	public MODOMaterialVersion MODOversion;

	[XmlElement ("Material")]
	public List<MODOMaterial> materials = new List<MODOMaterial> ();

	public MODOMaterial getMaterial (string name) {
		return materials.Find (mat => mat.name == name);
	}
}

/*
* ASSET POSTPROCESSOR
*/

public class MODOMaterialImporter : AssetPostprocessor {

	/*
	* XML LOADERS
	*/

	static MODOMaterialContainer LoadMaterialXMLFromStringReader (StringReader stringReader)
	{
		MODOMaterialContainer matContainer = null;
		XmlSerializer serializer = new XmlSerializer(typeof(MODOMaterialContainer));
		try
		{
			matContainer = serializer.Deserialize (stringReader) as MODOMaterialContainer;
		}
		catch (System.Exception e) {
			if (e != null) {} // Stop Unity complaining about unused variables.
			matContainer = null;
		}
		return matContainer;
	}

	static MODOMaterialContainer LoadMaterialXMLFromString(string content)
	{
		MODOMaterialContainer matContainer = null;
		if ((content != null) && (content != "")) {
			matContainer = LoadMaterialXMLFromStringReader (new StringReader(content));
		}
		return matContainer;
	}

	static MODOMaterialContainer LoadMaterialXMLFromPath (string path)
	{
		MODOMaterialContainer matContainer = null;
		TextAsset xmlAsset = AssetDatabase.LoadAssetAtPath(path, typeof(TextAsset)) as TextAsset;
		if ((xmlAsset != null) && (xmlAsset.text != "")) {
			matContainer = LoadMaterialXMLFromStringReader (new StringReader(xmlAsset.text));
		}
		return matContainer;
	}

	#if MODO_COMPATIBLE_UNITY_VERSION

	// Globals & Constants.
	string projectPath = Directory.GetCurrentDirectory();
	const string materialDir = "Materials";
	const string textureDir = "Textures";
	const string shaderName = "Standard";

	// Debug & User Options.
	static private bool alwaysApply = EditorPrefs.GetBool("MODOMaterialAlwaysApply", true);
	static private bool alwaysImport = EditorPrefs.GetBool("MODOMaterialAlwaysImport", false);
	static private bool debug = EditorPrefs.GetBool("MODOMaterialDebug", false);
	static void DebugLog (string message) {
		if (debug) {
			Debug.Log ("MODO Material Importer\n" + message);
		}
	}

	// Check if this is using the required Unity version.
	static private bool UnityVersionMin (int min) {
		string majorVerStr = Application.unityVersion.Split('.')[0];
		int majorVer = 0;
		if (int.TryParse (majorVerStr, out majorVer)) {
			return (majorVer >= min);
		}
		return false;
	}
	static bool isUnity5 = UnityVersionMin (5);

	static MODOMaterialPendingTextureContainer pendingTextures = new MODOMaterialPendingTextureContainer ();

	struct MaterialParameter
	{
		private readonly string m_property;
		private readonly string m_slot;
		private readonly string m_valueSlot;
		private readonly string m_channel;

		public MaterialParameter (string property, string slot=null, string valueSlot=null, string channel=null)
		{
			m_property = property;
			m_slot = slot;
			m_valueSlot = valueSlot;
			m_channel = channel;
		}

		public string Property { get { return m_property; } }
		public string Slot { get { return m_slot; } }
		public string ValueSlot { get { return m_valueSlot; } }
		public string Channel { get { return m_channel; } }
	}

	// Map the MODO material properties to Unity shader slots.
	static readonly List<MaterialParameter> materialParameters = new List<MaterialParameter>
	(new[] {
			//						MODO Property			Unity Texture Slot		Unity Scalar Slot			Channel
			new MaterialParameter ("Albedo",				"_MainTex",				null,						null),
			new MaterialParameter ("Normal",				"_BumpMap",				null,						null),
			new MaterialParameter ("Normal Scale",			null,					"_BumpScale",				null),
			new MaterialParameter ("Ambient Occlusion",		"_OcclusionMap",		null,						"Green"),
			new MaterialParameter ("Emission",				"_EmissionMap",			null,						null),
			//Emissive level is pre-multiplied into the emission color further up.
			//new MaterialParameter ("Emissive Level",		null,					"_EmissionColor",			null),
			new MaterialParameter ("Detail Mask",			"_DetailMask",			null,						"Alpha"),
			new MaterialParameter ("Detail Albedo x2",		"_DetailAlbedoMap",		null,						null),
			new MaterialParameter ("Detail Normal",			"_DetailNormalMap",		null,						null),
			new MaterialParameter ("Detail Normal Scale",	null,					"_DetailNormalMapScale",	null),
			new MaterialParameter ("Metallic",              "_MetallicGlossMap",	"_Metallic",				"Red"),
			new MaterialParameter ("Bump",					"_ParallaxMap",			null,						"Green"),
			new MaterialParameter ("Height Scale",			null,					"_Parallax",				null),
			new MaterialParameter ("Smoothness",            "_MetallicGlossMap",	"_Glossiness",				"Alpha")
	});

	/*string allForwardSlashes (string path) {
		return path.Replace (Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
	}*/

	public static string NormalizePath (string path) {
		return path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
	}

	// Given a directory path, return a list of all of the directories leading up to it.
	// Ordered from the furthest up to the directory itself.
	List<string> recurseDirectories (string directory) {
		List<string> directoryList = new List<string> ();
		char[] separators = { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
		string[] pathParts = directory.Split(separators);
		for (int numParts = pathParts.Length; numParts > 0; numParts--) {
			string temp_path = pathParts[0];
			for (int i = 1; i<numParts; i++) {
				temp_path = Path.Combine(temp_path, pathParts[i]);
			}
			directoryList.Add(temp_path);
		}
		return directoryList;
	}

	// Given a string, attempt to parse it into an array of floats.
	// Return success state, with output filled with the values and outputCount filled with the number of elements in the array.
	public static bool parseNumericValue (string input, float[] output, out int outputCount) {
		string[] splitStr = {", "};
		string[] elements = input.Split (splitStr, System.StringSplitOptions.RemoveEmptyEntries);
		int eleCount = Mathf.Min(elements.Length, output.Length);
		bool success = false;
		for (int e = 0; e < eleCount; e++) {
			success = float.TryParse(elements[e], out output[e]);
			if (!success) {
				break;
			}
		}
		outputCount = (success) ? eleCount : 0;
		return success;
	}

	// Given a texture filename and a list of paths, attempt to find an existing Texture asset that matches.
	Texture findExistingTexture (List<string> pathsToAssetDirectory, string textureFile) {
		Texture tex = null;

		// Look in the Textures directory of each directory up to root Assets directory.
		foreach (string test_path in pathsToAssetDirectory)
		{
			string test_assetPath = NormalizePath(Path.Combine(Path.Combine(test_path, textureDir), textureFile));
			tex = AssetDatabase.LoadAssetAtPath(test_assetPath, typeof(Texture)) as Texture;
			if (tex != null) {
				return tex;
			}
		}

		// Otherwise find any texture with this name in the entire project.

		// AssetDatabase.FindAssets crashes here if this is a fresh import, so we need to do this manually until Unity fix it.
		// This is the FindAssets path, leaving here for when the bug is fixed.
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
		string temp_path = Path.Combine ("Assets", textureDir);
		tex = AssetDatabase.LoadAssetAtPath (NormalizePath(Path.Combine (temp_path, textureFile)), typeof(Texture)) as Texture;
		if (tex != null) {
			return tex;
		}
		foreach (string d in Directory.GetDirectories ("Assets", "*", SearchOption.AllDirectories)) {
			if (d.EndsWith (textureDir, System.StringComparison.CurrentCultureIgnoreCase)) {
				continue;
			}
			temp_path = Path.Combine (d, textureDir);
			tex = AssetDatabase.LoadAssetAtPath (NormalizePath(Path.Combine (temp_path, textureFile)), typeof(Texture)) as Texture;
			if (tex != null) {
				return tex;
			}
		}

			// Otherwise return null.
		return null;
	}

	// Given a MODOMaterialPropertyTexture, asset directory paths, texture root path, find that texture or load in the external one defined in the property.
	Texture loadTextureFromProperty (MODOMaterialPropertyTexture matProp, List<string> pathsToAssetDirectory, string textureRootPath, bool force, out string texturePath) {
		if (matProp.filename != null) {
			string textureFile = Path.GetFileName (matProp.filename);
			string assetDirectory = pathsToAssetDirectory[Mathf.Max(pathsToAssetDirectory.Count - 1, 0)];

			// If the texture already exists (by Unity's search standards) use it.
			Texture tex = findExistingTexture(pathsToAssetDirectory, textureFile);
			if (!force && (tex != null)) {
				texturePath = NormalizePath(AssetDatabase.GetAssetPath (tex));
				return tex;
			}

			// Otherwise load in the texture from the absolute path, if it exists.
			
			// External file path.
			string importPath = NormalizePath(Path.Combine(textureRootPath, matProp.filename));

			// Check that the extnernal texture file exists before we do anything.
			if (File.Exists (importPath)) {
				string importedDirectory = NormalizePath(Path.Combine (assetDirectory, "Textures"));
				// If the Textures directory doesn't exist, then create it.
				if (!Directory.Exists (importedDirectory) && !AssetDatabase.IsValidFolder (importedDirectory)) {
					importedDirectory = NormalizePath(AssetDatabase.GUIDToAssetPath (AssetDatabase.CreateFolder (assetDirectory, "Textures")));
				}

				// Get the path of where the imported file will wind up, relative to the project directory.
				string importedFile = NormalizePath(Path.Combine(importedDirectory, textureFile));

				if (importExternalFile(importPath, importedFile)) {
					AssetDatabase.ImportAsset(importedFile, ImportAssetOptions.ForceUpdate);
					texturePath = importedFile;
				}
				else {
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
	bool importExternalFile (string externalPath, string internalPath) {
		// Check we're not trying to overwrite a file with itself.
		if (string.Compare (NormalizePath(externalPath), NormalizePath(Path.Combine(projectPath, internalPath)), true) == 0)
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

	// Figure out the target material name, based on the user's import preferences for the mesh asset.
	string getMaterialName (string modelName, ModelImporterMaterialName materialName, Material material) {
		if (materialName == ModelImporterMaterialName.BasedOnMaterialName) {
			return material.name;
		} else if (materialName == ModelImporterMaterialName.BasedOnModelNameAndMaterialName) {
			return (modelName + "-" + material.name);
		} else if (materialName == ModelImporterMaterialName.BasedOnTextureName) {
			Texture mainTex = material.GetTexture ("_MainTex");
			if (mainTex != null) {
				return mainTex.name;
			}
			else {
				return material.name;
			}
		}
		return material.name;
	}

	// Check through the Assets directory to find if this material already exists.
	string getMaterialPath (List<string> pathsToAssetDirectory, string importedMaterialFile, string importedMaterialName, ModelImporterMaterialSearch materialSearch, out bool materialAlreadyExists) {
		// Default to not already existing.
		materialAlreadyExists = false;
		string path = pathsToAssetDirectory[Mathf.Max(pathsToAssetDirectory.Count - 1, 0)];

		if (materialSearch == ModelImporterMaterialSearch.Local) {
			// Look just in the local Materials directory.
			string temp_path = Path.Combine (path, materialDir);
			if (AssetDatabase.LoadAssetAtPath (Path.Combine (temp_path, importedMaterialFile), typeof (Material))) {
				materialAlreadyExists = true;
			}
			return temp_path;
		} else if (materialSearch == ModelImporterMaterialSearch.RecursiveUp) {
			// Look in the Materials directory of each directory up to root Assets directory.
			// Fall back to the local Materials directory if not found.
			foreach (string test_path in pathsToAssetDirectory) {
				string temp_path = Path.Combine (test_path, materialDir);
				if (AssetDatabase.LoadAssetAtPath (Path.Combine (temp_path, importedMaterialFile), typeof (Material))) {
					materialAlreadyExists = true;
					return temp_path;
				}
			}
		} else if (materialSearch == ModelImporterMaterialSearch.Everywhere) {
			// Look across the entire project for a material with this name, grab the first result.
			// Fall back to the local Materials directory if not found.
			string[] guids = AssetDatabase.FindAssets (importedMaterialName + " t:material", null);
			if (guids.Length > 0) {
				materialAlreadyExists = true;
				return AssetDatabase.GUIDToAssetPath (guids[0]);
			}
		}
		return Path.Combine (path, materialDir);
	}

	// Set up the blend modes for the material.
	// Copied from StandardShaderGUI.cs.
	public enum BlendMode
	{
		Opaque,
		Cutout,
		Fade,		// Old school alpha-blending mode, fresnel does not affect amount of transparency
		Transparent // Physically plausible transparency mode, implemented as alpha pre-multiply
	}

	// Set up the blend modes for the material.
	// Copied from StandardShaderGUI.cs.
	void SetupMaterialWithBlendMode(Material material, BlendMode blendMode)
	{
		switch (blendMode)
		{
		case BlendMode.Opaque:
			material.SetOverrideTag("RenderType", "");
			material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
			material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
			material.SetInt("_ZWrite", 1);
			material.DisableKeyword("_ALPHATEST_ON");
			material.DisableKeyword("_ALPHABLEND_ON");
			material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
			material.renderQueue = -1;
			break;
		case BlendMode.Cutout:
			material.SetOverrideTag("RenderType", "TransparentCutout");
			material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
			material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
			material.SetInt("_ZWrite", 1);
			material.EnableKeyword("_ALPHATEST_ON");
			material.DisableKeyword("_ALPHABLEND_ON");
			material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
			material.renderQueue = 2450;
			break;
		case BlendMode.Fade:
			material.SetOverrideTag("RenderType", "Transparent");
			material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
			material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
			material.SetInt("_ZWrite", 0);
			material.DisableKeyword("_ALPHATEST_ON");
			material.EnableKeyword("_ALPHABLEND_ON");
			material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
			material.renderQueue = 3000;
			break;
		case BlendMode.Transparent:
			material.SetOverrideTag("RenderType", "Transparent");
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

	Material OnAssignMaterialModel (Material material, Renderer renderer) {
		if (!isUnity5) {
			return null;
		}

		// Check to see if there's an XML file to match with the FBX asset.
		string xmlPath = NormalizePath(Path.Combine(Path.GetDirectoryName(assetPath), Path.GetFileNameWithoutExtension(assetPath) + ".xml"));
		MODOMaterialContainer matContainer = LoadMaterialXMLFromPath(xmlPath);
		if (matContainer == null) {
			// Unity will default back to it's own usual methods if no material XML file exists.
			return null;
		}

		// Here, the material XML file is valid and has been loaded.

		// First parse the paths to get some names and directories to search later.
		// Get the mesh asset directory and name.
		string assetDirectory			= Path.GetDirectoryName (assetPath);
		DebugLog("Asset Directory: " + assetDirectory);

		string assetName				= Path.GetFileNameWithoutExtension(assetPath);
		DebugLog("Asset Name: " + assetName);

		string textureRootPath = (matContainer.useRootPath == 1) ? matContainer.rootPath : Path.Combine(projectPath, assetDirectory);
		DebugLog("Texture Root Path: " + textureRootPath);

		// Get all the paths from here to root of Assets directory.
		List<string> pathsToAssetDirectory = recurseDirectories(assetDirectory);

		// Get the model's material search and name settings.
		ModelImporter				modelImporter	= assetImporter as ModelImporter;
		ModelImporterMaterialSearch	materialSearch	= modelImporter.materialSearch;
		ModelImporterMaterialName	materialName	= modelImporter.materialName;

		// Get the material name based on material name setting.
		string importedMaterialName = getMaterialName (assetName, materialName, material);

		// Path to the material file itself (with extension).
		string importedMaterialFile = importedMaterialName + ".mat";

		// The directory path (relative to the project root directory) of the material.
		// Figure out the material directory path based on the Mesh's material search setting.
		// The found variable is true if an existing material was found at that path. False if a material needs to be created.
		bool   materialAlreadyExists = false;
		string importedMaterialPath  = getMaterialPath (pathsToAssetDirectory, importedMaterialFile, importedMaterialName, materialSearch, out materialAlreadyExists);

		// The full path to the material file (relative to the project root directory).
		string materialPath = NormalizePath(Path.Combine (importedMaterialPath, importedMaterialFile));

		// If there's a material here already and we're not always updating the values, just return that material.
		if (AssetDatabase.LoadAssetAtPath (materialPath, typeof(Material))) {
			material = AssetDatabase.LoadAssetAtPath (materialPath, typeof(Material)) as Material;
			if (!alwaysApply) {
				return material;
			}
		}

		// Apply the PBR shader to the material if it's not already applied.
		Shader pbrShader = Shader.Find(shaderName);
		if ((pbrShader != null) && ((material.shader == null) || (material.shader != pbrShader))) {
			material.shader = pbrShader;
		}
		// Check the shader has been properly applied, error out if not.
		if (material.shader == null) {
			DebugLog("Unable to find shader: " + shaderName);
			return null;
		}

		// Get the material definition from the material container.
		MODOMaterial mat = matContainer.getMaterial (material.name);
		if (mat == null) {
			DebugLog("XML file has no definition for material" + material.name);
			return null;
		}

		// Set diffuse color of the material.
		// And if opacity is less than 1, then set the material blend mode to fade (z-primed transparent).
		Color diffuseColor = mat.getDiffuseColor ();
		material.SetColor ("_Color", diffuseColor);
		DebugLog ("Setting " + mat.name + ": Color to " + diffuseColor);
		if (diffuseColor.a < 1.0f) {
			SetupMaterialWithBlendMode (material, BlendMode.Fade);
			DebugLog ("Setting " + mat.name + ": to Fade.");
		}

		// Set emissive color & level of the material.
		Color emissiveColor = mat.getEmissiveColor ();
		if (emissiveColor.maxColorComponent > (0.1f / 255.0f)) {
			material.EnableKeyword ("_EMISSION");
		}
		material.SetColor ("_EmissionColor", emissiveColor);
		DebugLog ("Setting " + mat.name + ": Emission Color to " + emissiveColor);

		// For each material slot, load up the information for it.
		// If there is no texture defined, fall back to the scalar/vector value.
		// Any textures that need to be imported are added to a pending list to be applied after they've been imported.
		foreach (MaterialParameter parameter in materialParameters) {
			MODOMaterialProperty matProp = mat.getProperty (parameter.Property);
			if (matProp == null) {
				continue;
			}

			// Attempt to load in a texture for the slot.
			if (parameter.Slot != null) {
				string texturePath = null;
				string textureChannel = null;
				Texture tex = null;

				bool validTexture = false;
				//bool skippedApplication = false;

				// Iterate through each of the textures listed and find the first valid one.
				foreach (MODOMaterialPropertyTexture matPropTexture in matProp.textures) {
					tex = loadTextureFromProperty(matPropTexture, pathsToAssetDirectory, textureRootPath, alwaysImport, out texturePath);

					// Texture doesn't exist yet, so import it.
					if ((tex == null) && (texturePath != null)) {
						if (pendingTextures.addPendingTexture(material, texturePath, parameter.Slot)) {
							DebugLog("Deferred setting " + mat.name + ": " + matProp.name + " to " + texturePath);
							textureChannel = matPropTexture.channel;
						} else {
							DebugLog(mat.name + ": " + matProp.name + " already has a texture waiting to be applied, skipping application of " + texturePath);
							//skippedApplication = true;
						}
					}

					// Set up the wrap values for the main maps.
					if (parameter.Property == "Albedo") {
						Vector2 scale = matPropTexture.getWrapUV (material.GetTextureScale(parameter.Slot));
						DebugLog("Setting " + mat.name + ": " + parameter.Property + " Wrap UV to (" + scale.x + ", " + scale.y + ").");
						material.SetTextureScale(parameter.Slot, scale);
					}

					// Set up the wrap values for the detail maps.
					if (parameter.Property == "Detail Albedo x2") {
						if (matPropTexture.uvmap != null) {
							if (matPropTexture.uvmap == "UV1") {
								material.SetFloat("_UVSec", 1.0f);
								DebugLog("Setting " + mat.name + ": Secondary UV to UV1");
							}
							else {
								material.SetFloat("_UVSec", 0.0f);
								DebugLog("Setting " + mat.name + ": Secondary UV to UV0");
							}
						}
						Vector2 scale = matPropTexture.getWrapUV (material.GetTextureScale(parameter.Slot));
						DebugLog("Setting " + mat.name + ": " + parameter.Property + " Wrap UV to (" + scale.x + ", " + scale.y + ").");
						material.SetTextureScale(parameter.Slot, scale);
					}

					// Check to see if there are any conflicts with the Metallic/Smoothness maps.
					// Users might assign them differently in MODO but Unity is much more fussy and wants
					// them to be derived from the same texture.
					if (parameter.Property == "Smoothness") {
						string metalPath = null;

						// Check to see if the material already has a metalness texture applied.
						Texture metalTexture = material.GetTexture(parameter.Slot);

						// A Metalness texture isn't already applied, so search the pending textures to see if there's one waiting to be applied.
						if (metalTexture == null) {
							foreach (MODOMaterialPendingTexture pendingTexture in pendingTextures.getPendingTextures(material)) {
								if (pendingTexture.textureSlot == parameter.Slot) {
									metalPath = pendingTexture.texturePath;
									break;
								}
							}
						}
						else {
							metalPath = AssetDatabase.GetAssetPath(metalTexture);
						}

						if (metalPath != null) {
							if (tex != null) {
								if (metalPath != AssetDatabase.GetAssetPath(tex)) {
									// Metallic texture is set, but different smoothness texture is destined to be applied to the same slot.
									Debug.LogWarning("MODO Material Importer\nThe textures specified for " + mat.name + "'s Metallic and Smoothness don't match!\nUnity expects to read the Metallic value from the red channel and Smoothness from the alpha channel of the same texture! Your results may not be as expected.");
								}
							}
							else if (texturePath != null) {
								if (string.Compare (metalPath, texturePath, true) != 0) {
									// Metallic texture is set, but different smoothness texture is destined to be applied to the same slot.
									Debug.LogWarning("MODO Material Importer\nThe textures specified for " + mat.name + "'s Metallic and Smoothness don't match!\nUnity expects to read the Metallic value from the red channel and Smoothness from the alpha channel of the same texture! Your results may not be as expected.");
								}
							}
							else {
								// Metallic texture is set, but no smoothness texture is set.
								// Unity will ignore the scalar Smoothness value supplied by the user and read it from the alpha of the metallic texture.
								Debug.LogWarning("MODO Material Importer\n" + mat.name + "'s Metallic value is being driven by a texture but Smoothness is not.\nUnity will attempt to read the smoothness value from the alpha channel of the Metallic texture, not the value specified! Your results may not be as expected.");
							}
						}
						else if ((metalPath == null) && ((tex != null) || (texturePath != null))) {
							// No metallic texture is set, but smoothness texture is destined to be applied to the same slot.
							// Unity will ignore the scalar Metalness value supplied by the user and read it from the red of the metallic texture.
							Debug.LogWarning("MODO Material Importer\n" + mat.name + "'s Smoothness value is being driven by a texture but Metalness is not.\nUnity will attempt to read the metallic value from the red channel of the Smoothness texture, not the value specified! Your results may not be as expected.");
						}
					}

					if ((tex != null) || (texturePath != null)) {
						// We found a texture.
						validTexture = true;
						if (tex != null) {
							material.SetTexture(parameter.Slot, tex);
							DebugLog("Setting " + mat.name + ": " + matProp.name + " to " + tex.name);
						}

						// Alert the user if their texture channels are mis-wired.
						if ((textureChannel != null) && (parameter.Channel != null)) {
							if (string.Compare(textureChannel, parameter.Channel, true) != 0) {
								Debug.LogWarning("MODO Material Importer\n" + mat.name + "'s " + parameter.Property + " texture value is being driven by the " + textureChannel + " channel of the texture.\nUnity wants to take it from the " + parameter.Channel + " channel! Your results may not be as expected.");
							}
						}
						break;
					}
				}

				if (!validTexture) {
					material.SetTexture (parameter.Slot, null);
					DebugLog("Clearing texture for " + mat.name + ": " + parameter.Slot);
				}
			}

			// Fill the scalar value slot, if it has one.
			if (parameter.ValueSlot != null) {
				if (matProp.value != null) {
					float[] floatValues = new float[4];
					int floatCount = 0;
					parseNumericValue (matProp.value, floatValues, out floatCount);
					if (floatCount == 1) {
						// Scalar channel.
						DebugLog ("Setting " + mat.name + ": " + matProp.name + " to " + floatValues[0]);
						material.SetFloat (parameter.ValueSlot, floatValues[0]);
					}
					else if (floatCount > 1) {
						// Vector channel.
						Vector4 vectorValue = material.GetVector (parameter.ValueSlot);
						for (int i = 0; i < floatCount; i++) {
							vectorValue[i] = floatValues[i];
						}
						material.SetVector (parameter.ValueSlot, vectorValue);
						DebugLog ("Setting " + mat.name + ": " + matProp.name + " to " + vectorValue);
					}
				}
			}
		}

		// Create the directory for the material - and the material asset - if it doesn't already exist.
		if (!materialAlreadyExists) {
			if (!Directory.Exists (importedMaterialPath) && !AssetDatabase.IsValidFolder (importedMaterialPath)) {
				AssetDatabase.CreateFolder (assetDirectory, materialDir);
			}
			// Create the material itself.
			AssetDatabase.CreateAsset (material, materialPath);
		}

		// Return the material.
		return material;
	}

	// Handle any textures that weren't already imported at material creation time.
	// Here we're just ensuring that textures going into normal map slots are set as normal maps.
	void OnPreprocessTexture () {
		if (!isUnity5) {
			return;
		}

		// Get all the textures at the path of this one.
		// Set them to be normal maps.
		foreach (MODOMaterialPendingTexture pendingTexture in pendingTextures.getPendingTextures (assetPath)) {
			if ((pendingTexture.textureSlot == "_BumpMap") || (pendingTexture.textureSlot == "_DetailNormalMap")) {
				TextureImporter textureImporter = assetImporter as TextureImporter;
				if (textureImporter.textureType != TextureImporterType.Bump) {
					textureImporter.textureType = TextureImporterType.Bump;
				}
			}
		}
	}

	// Handle any textures that weren't already imported at material creation time.
	void OnPostprocessTexture (Texture texture) {
		if (!isUnity5) {
			return;
		}
		// Get all the textures at the path of this one.
		// And tell them they're ready to be applied to their materials.
		foreach (MODOMaterialPendingTexture pendingTexture in pendingTextures.getPendingTextures (assetPath)) {
			pendingTexture.state = MODOPendingState.ReadyToApply;
		}
		// The material will have the slot filled after loading has finished.
		EditorApplication.delayCall += pendingTextures.applyTexture;
	}


	/*
	* POSTPROCESSOR
	*/

	static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) {
		if (!isUnity5) {
			return;
		}
		// Check to see if we've just imported a MODO Material XML file.
		// If we have, and we haven't also just imported it's respective FBX file, then re-import the FBX file.
		// That will force the AssetImporter to update the material values to whatever's been changed in this XML file.
		foreach (var str in importedAssets) {
			if (Path.GetExtension(str) == ".xml") {
				MODOMaterialContainer matContainer = LoadMaterialXMLFromPath(str);
				if (matContainer != null) {
					string modelPath = Path.Combine(Path.GetDirectoryName(str), Path.GetFileNameWithoutExtension(str) + ".fbx");
					if ((!importedAssets.Contains(modelPath)) && (AssetDatabase.LoadAssetAtPath(modelPath, typeof(GameObject)) != null)) {
						AssetDatabase.ImportAsset(modelPath, ImportAssetOptions.ForceUpdate);
						DebugLog("Reimporting model.");
					}
					else {
						DebugLog("Model was just reimported with this XML file, so not reimporting.");
					}
				}
			}
		}
	}
	#endif

	// Make a nicer Inspector for the MODO Material XML file.
	// Default to the Default Inspector if it's not a MODO Material XML file.
	[CustomEditor(typeof(TextAsset))]
	public class TextAssetEditor : Editor {

		#if MODO_COMPATIBLE_UNITY_VERSION
		bool showVersion	= false;
		List<bool> toggles = new List<bool>();
		#endif

		public void showContent (TextAsset textAsset) {
			EditorGUILayout.TextArea (textAsset.text, GUILayout.ExpandHeight (true), GUILayout.ExpandWidth (true));
		}

		public override void OnInspectorGUI() {

			TextAsset textAsset = target as TextAsset;
			MODOMaterialContainer matContainer = LoadMaterialXMLFromString(textAsset.text);
			if (matContainer == null) {
				showContent (textAsset);
			}
			else {
				#if MODO_COMPATIBLE_UNITY_VERSION
				// Enable the GUI so you can toggle bits and pieces.
				GUI.enabled = true;

				EditorGUILayout.LabelField("Global MODO Material Importer Settings", EditorStyles.boldLabel);
				alwaysApply = EditorGUILayout.Toggle("Always Update Materials", alwaysApply);
				EditorGUI.BeginDisabledGroup (alwaysApply == false);
				alwaysImport = EditorGUILayout.Toggle("Always Reimport External Textures", alwaysImport);
				EditorGUI.EndDisabledGroup ();
				debug = EditorGUILayout.Toggle("Show Debug Info", debug);

				EditorGUILayout.Separator();

				EditorGUILayout.LabelField("MODO Material File", EditorStyles.boldLabel);

				if (GUILayout.Button("Force Reimport (using Global Settings)")) {
					string assetPath = AssetDatabase.GetAssetPath(target);
					AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
				}

				EditorGUILayout.Separator();

				EditorGUILayout.LabelField("MODO Material File Contents", EditorStyles.boldLabel);

				if (matContainer.useRootPath == 1) {
					EditorGUILayout.LabelField("Root Texture Path: " + matContainer.rootPath);
				}

				for (int i = toggles.Count; i < matContainer.materials.Count; i++) { toggles.Add(false); }
				int toggleIdx = 0;
				foreach (MODOMaterial mat in matContainer.materials) {
					toggles[toggleIdx] = EditorGUILayout.Foldout(toggles[toggleIdx], mat.name);
					if (toggles[toggleIdx]) {
						foreach (MaterialParameter parameter in materialParameters) {
							MODOMaterialProperty matProp = mat.getProperty(parameter.Property);
							if (matProp != null) {
								if (
									((matProp.numTextures() > 0) && (parameter.Slot != null))
									||
									((matProp.value != null) && (parameter.ValueSlot != null))
									)
								{
									EditorGUILayout.BeginVertical();
									EditorGUILayout.LabelField(matProp.name, EditorStyles.boldLabel);

									if ((parameter.Slot != null) && (matProp.numTextures() > 0)) {
										foreach (MODOMaterialPropertyTexture matPropTexture in matProp.textures) {
											EditorGUILayout.BeginVertical();
											EditorGUILayout.LabelField(matPropTexture.name);
											if (matPropTexture.filename != null) {
												EditorGUILayout.LabelField("Texture", matPropTexture.filename);
											}
											if ((matPropTexture.wrapU != null) && (matPropTexture.wrapV != null)) {
												EditorGUILayout.LabelField("Tiling", matPropTexture.wrapU + ", " + matPropTexture.wrapV);
											}
											else if (matPropTexture.wrapU != null) {
												EditorGUILayout.LabelField("Tiling U", matPropTexture.wrapU);
											}
											else if (matPropTexture.wrapV != null) {
												EditorGUILayout.LabelField("Tiling V", matPropTexture.wrapV);
											}
											if (matPropTexture.uvmap != null) {
												EditorGUILayout.LabelField("UVMap", matPropTexture.uvmap);
											}
											EditorGUILayout.EndVertical();
										}
									}
									if ((parameter.ValueSlot != null) && (matProp.value != null)) {
										EditorGUILayout.LabelField("Numeric Value", matProp.value);
									}
									EditorGUILayout.EndVertical();
								}
							}
						}
					}
					toggleIdx++;
				}

				EditorGUILayout.Separator();
				if (debug) {
					EditorGUILayout.Separator();
					showVersion = EditorGUILayout.Foldout(showVersion, "Debug Info");
					if (showVersion) {
						EditorGUILayout.BeginVertical();
						if (matContainer.MODOversion != null) {
							EditorGUILayout.LabelField("App: " + matContainer.MODOversion.app);
							EditorGUILayout.LabelField("Version: " + matContainer.MODOversion.version);
							EditorGUILayout.LabelField("Build: " + matContainer.MODOversion.build);
						}
						EditorGUILayout.EndVertical();
					}
				}

				if (GUI.changed)
				{
					EditorPrefs.SetBool("MODOMaterialAlwaysApply", alwaysApply);
					EditorPrefs.SetBool("MODOMaterialAlwaysImport", alwaysImport);
					EditorPrefs.SetBool("MODOMaterialDebug", debug);
				}
				#else
					EditorGUILayout.LabelField ("MODO Material Importer is for Unity 5+ Only", EditorStyles.boldLabel);
				#endif
			}
		}
	}
}
