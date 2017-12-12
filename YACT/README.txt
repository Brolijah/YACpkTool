Yet Another CPK Tool, written by Brolijah

 DISCLAIMER: THIS IS A WORK IN PROGRESS BUILD
The only thing I haven't completed is in-CPK file replacement yet. It's still in 
the works, but I hope to have it done soon. It should be possible albeit with some 
caveats as described in CriWare's user manual.

YACpkTool's HELP information:

The purpose of this tool is only to interface with CpkMaker.dll (Copyright CRI Middleware Co)
Some use cases were inspired by CriPakTools.

Simple drag-n-drop-like usage:
  EXTRACT: YACpkTool.exe INPUT_CPK [OUT_FOLDER]
  REPACK : YACpkTool.exe IN_FOLDER [OUT_CPK_FILE]
  Examples:
    YACpkTool.exe data0.cpk                  Extracts contents of data0.cpk to folder /data0/
    YACpkTool.exe data0.cpk data_out         Extracts contents of data0.cpk to folder /data_out/
    YACpkTool.exe data0_out                  Packs the contents of data0_out to data0_out.cpk
    YACpkTool.exe data0_out new_data.cpk     Packs the contents of data0_out to new_data.cpk

Technical CLI usage:
  YACpkTool.exe [options] {arguments}
  commands:
    -X {file}   Extracts files. Optional argument: A specific file
    -P          Packages a folder to a CPK.
    -R {file}(in cpk) {file}(in dir)
                STILL WIP! DISABLED!
                Replaces a specified file in the CPK.
  options:
    -h          Displays this help information + examples + about info
    -l          Lists each file contained or being processed (TODO). Doubles as a command (Complete).
    -v          Displays technical info about the running process (TODO).
    -i {name}   Your input file or folder name (REQUIRED FOR ALL OPERATIONS)
    -o {name}   Your output file or folder name (relative or absolute)
    -d {path}   Directory name. If specified, extraction and/or packaging will search here instead.

  packing options (only use if you know what to do):
    --codec {name}    Compression codec to use.
                      Default is none. Available options: none, layla
    --align {size}    Data alignment of the CPK.
                      Default is 2048. Available options: Powers of 2 between 1 and 32768.

  Examples:
    Listing contents (could use refinement):
      YACpkTool.exe -l -i data0.cpk
      YACpkTool.exe -l -v -i data0.cpk
    Extraction:
      YACpkTool.exe -X fol/inside/cpk/thing -i "X:\path\to\cpk\data0.cpk"
      YACpkTool.exe -X -d "X:\path\to\cpk" -i data0.cpk
      YACpkTool.exe -X -i data0.cpk
    Packing:
      YACpkTool.exe -P -d "X:\path\to\contents\" -i folder -o new_package.cpk
      YACpkTool.exe -P -i "X:\path\to\contents\folder\"
      YACpkTool.exe -P -i folder --codec LAYLA --align 2048
