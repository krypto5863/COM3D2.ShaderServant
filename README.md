# ShaderServant
A lightweight, high performance alternative to NPRShader. This point is to provide a simple and non-intrusive external shader loader while also supporting the old NPR standard.

# Usage
1. Just throw the two files into `bepinex/plugins`
2. Place shader packages in `ShaderServantPacks`
3. Go.

Requires 2.34+, currently no support for 3.0/COM 2.5

# Advanced Mates
This plugin seeks to be a simple and flexible external shader loader. So apart from loading all of NPRShader's shaders, it also can load any shader packages placed into ShaderServantPacks.
However, in doing this, we saw there was a need for a more efficient and flexible way of toggling keywords and setting cubemaps. This is our advanced mates format that only works with ShaderServant.

If you wish to use advanced material functions, keep reading.

## Remove `_NPRMAT_` from file name!
NPR material files are denoted by using `_NPRMAT_SomeShaderName` in order to load shaders. However, when SS(ShaderServant) detects `_NPRMAT_` it will internally convert it to an advanced material but if you mix the keyword with advanced material functions, it will produce errors. Your advanced materials should be named just like your typical materials. Whatever you want, just no `_NPRMAT_`

## Set Shaders
NPR used the shader fields within the mate files as a fallback and relied on the file naming convention to fetch the shader it wanted. But this creates undue complexity for little return. We instead use old reliable and fetch the shaders the old fashioned way.

Shader Name: `com3d2mod/Standard_NPRToon_`
Material Template Name: `com3d2mod_NPRToon_`

### Material Template?
When modders add shaders into a shader package, they should include a "default" material that uses said shader. This material contains the values that will be used initially when the material is created. Afterwards, any changes defined in the mate files are set.

Obviously this means a modder can define the entire material in the package and use a barebones mate, or do the opposite and use a default material and a very long and complex mate. We suggest the former.

## Toggling Keywords
Add a new float property to your material file. The name of this property will be the Keyword with the SS toggle keyword added. For example:
`_UseReflections_SSKEYWORD`
`_InterpolateMatcaps_SSKEYWORD`
`_PreferLeeches_SSKEYWORD`
Your float value itself should be 0 or 1:
1 keyword enabled. 
0 keyword disabled.

**May cause issues if you leave the `_NPRMAT_` keyword**

## Using Cubemaps
Cubemaps are special textures that need to be converted internally to cubemaps and then set to their appropriate property. Sadly, there is no way to detect when a property takes a cubemap, so you will need to manually tell SS that this texture should be loaded as a cubemap. To do so, simply change the texture type from `tex2d` to `cube`.

**Will not work if you leave the `_NPRMAT_` keyword**
