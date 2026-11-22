# Third-party components

Proctal bundles the unmodified Windows x64 .NET runtime and Windows Desktop runtime from the official project-local SDK. Their license and notices are included in `.runtime/LICENSE.txt` and `.runtime/ThirdPartyNotices.txt`.

Application dependencies: NAudio 2.2.1 (MIT), PdfPig 0.1.13 (Apache-2.0), System.Security.Cryptography.ProtectedData 10.0.0 (MIT). Development-only dependencies: xUnit 2.9.3, xUnit Visual Studio runner 3.1.5, Microsoft.NET.Test.Sdk 18.0.1. Exact transitive versions are in the checked-in `packages.lock.json` files.

Project pages: https://github.com/naudio/NAudio · https://github.com/UglyToad/PdfPig · https://github.com/dotnet/runtime · https://github.com/xunit/xunit

IBM Plex Sans and IBM Plex Mono are embedded, without global font installation, under the SIL Open Font License. Full license: `Assets/Fonts/OFL.txt`. Source URLs and Git blob SHA values: `src/Proctal.Desktop/Assets/Fonts/sources.json`. Source: https://github.com/IBM/plex.

Design guidance: Taste (https://github.com/Leonxlnx/taste-skill) and Impeccable (https://github.com/pbakaus/impeccable). Website-specific tools were not installed or used to certify native WPF code.

Native equations use WpfMath 2.1.0 and XamlMath.Shared 2.1.0 (MIT), with unmodified embedded Computer Modern math fonts (Knuth License) and jlm_msam10.ttf (SIL OFL). Full code and font notices are bundled in `Assets/Licenses/WpfMath-MIT.md` and `Assets/Licenses/WpfMath-fonts.md`. No fonts are installed globally. Source: https://github.com/ForNeVeR/xaml-math/tree/v2.1.0.

## Markdig 1.6.0 — BSD 2-Clause

Source: https://github.com/xoofx/markdig/blob/1.6.0/license.txt

Copyright (c) 2016-2026, Alexandre Mutel
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer.
2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
