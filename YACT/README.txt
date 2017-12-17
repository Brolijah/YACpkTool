Yet Another CPK Tool, written by Brolijah

Please note that file replacement is something still experimental. It needs some toying
around with to find out what does and what doesn't break the result CPK file. It should
be possible albeit with some caveats as described in CriWare's user manual. If you
encounter any unexpected behaviors with using the -R command, please open up an issue.

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
                EXPERIMENTAL! Replaces a specified file in the CPK.
    -L          Lists the file contents and some basic information about the CPK.
  options:
    -h          Displays this help information + examples + about info
    -v          Displays technical info about the running process.
    -i {name}   Your input file or folder name (REQUIRED FOR ALL COMMANDS)
    -o {name}   Your output file or folder name (relative or absolute)
    -d {path}   Directory name. If specified, extraction and/or packaging will search here instead.

  extra options:
    --csv {name}      A specified CSV file (relative or absolute)
                      Can be used to export a CSV or to read from a CSV (when applicable).
    --align {size}    Data alignment of the CPK.
                      Default is 2048. Available options: Powers of 2 between 1 and 32768.
    --codec {name}    (Packing only.) Compression codec to use.
                      Default is none. Available options: none, layla

  Examples:
    Listing contents:
      YACpkTool.exe -L -i data0.cpk
      YACpkTool.exe -L -v -i data0.cpk
    Extraction:
      YACpkTool.exe -X fol/in/cpk/thing.bin -i "X:\path\to\cpk\data0.cpk"
      YACpkTool.exe -X -d "X:\path\to\cpk" -i data0.cpk
      YACpkTool.exe -X -i data0.cpk
    Packing:
      YACpkTool.exe -P -d "X:\path\to\contents\" -i folder -o new_package.cpk
      YACpkTool.exe -P -i "X:\path\to\contents\folder\"
      YACpkTool.exe -P -i folder --codec LAYLA --align 2048
    Replacing:
      YACpkTool.exe -d "X:\path\to\cpk" -i data0.cpk -R fol/in/cpk/thing.bin new_thing.bin
      YACpkTool.exe -i "X:\path\to\cpk\data0.cpk" -R fol/in/cpk/thing.bin "X:\path\to\new\file.bin"
      YACpkTool.exe -i data0.cpk -R fol/in/cpk/thing.bin new_thing.bin -o data0_patched.cpk
