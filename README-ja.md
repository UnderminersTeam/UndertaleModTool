# UndertaleModTool

[English README is here](README.md)

[![Underminers Discord](https://img.shields.io/discord/566861759210586112?label=Discord&logo=discord&logoColor=white)](https://discord.gg/hnyMDypMbN) [![GitHub](https://img.shields.io/github/license/UnderminersTeam/UndertaleModTool?logo=github)](https://github.com/UnderminersTeam/UndertaleModTool/blob/master/LICENSE.txt)

（すばらしい　ツールを　まのあたりにして　ケツイがみなぎった）

どうも！Undertale/DeltaruneのようなGameMakerゲームを掘り下げるのが好きと聞いたので、おすすめのツールを紹介します！

![Flowey: Now YOU are the GOD of this world.](images/flowey.gif)


# ダウンロード

ツールの安定版と最新版は、以下からダウンロードできます。
UndertaleModToolの設定から、いつでも最新版にアップデートできます。

| リリース | 状態	 |
|:---:	|----------	|
| 安定版	| [![安定版リリース](https://img.shields.io/github/downloads/UnderminersTeam/UndertaleModTool/0.8.2.0/total)](https://github.com/UnderminersTeam/UndertaleModTool/releases/tag/0.8.2.0) |
| 最新版	| [![最新版リリース](https://img.shields.io/github/downloads/UnderminersTeam/UndertaleModTool/bleeding-edge/total)](https://github.com/UnderminersTeam/UndertaleModTool/releases/tag/bleeding-edge) |

UndertaleModToolはリリースごとにビルドの違いがある点にご注意ください。違いは以下の通りです。

* `.NET bundled` - ツールの実行に必要な.NETランタイムを同梱しています。安定版は全て.NETバンドル版なので、必要なランタイムをインストールする必要はありません。
* `Single file` - 実行ファイルが一つにまとまっています。全ての依存関係が組み込まれているので、フォルダがすっきりしますが、予期せぬ安定性の問題が発生することがあります。
* `Non-single File` - 全依存関係が実行ファイルの隣に配置されます。約300個のdllが入ったフォルダから実行ファイルを見つけるのが面倒でない、または`Single file`ビルドの安定性の問題がある場合はこちらを選択してください。

# 主な機能

* UndertaleやDeltaruneといったほとんどのGameMakerゲームのデータファイルから、すべてのバイトを読み取り、デコードされたデータからバイト単位で正確なコピーを再作成できます。
* ファイル内のすべてのポインタを正しく処理するため、要素の追加/削除、長さの変更、移動などを行ってもファイル形式が壊れません。
* すべての値（不明な値も含む）を変更できるエディター。
* シンプルなルームエディターを搭載。
* GML VMコードの編集が可能。これにより、組み込みのGMLコンパイラやGMLアセンブリを使用して、ゲームにカスタムコードを追加できます。（YYCはサポートされていません。）
* 高レベルのGML逆コンパイラとコンパイラ。幅広いGameMakerバージョンをサポートし、最も重要なGML機能のほとんどをサポートしています（まだいくつか不足しています）。
* 自動的にデータファイルを変更するスクリプトを実行できる機能（または他の不正なタスクを実行）。これにより、ファイルパッチやプロジェクトシステムなど、他の方法でのMOD配布が可能になります。
* すべてのコア機能を外部ツールで使用できるライブラリに抽出。
* `.yydebug`ファイルを生成して、GM:Sデバッガーで変数をライブ編集できます!(詳細 [ここをクリック](https://github.com/UnderminersTeam/UndertaleModTool/wiki/Corrections-to-GameMaker-Studio-1.4-data.win-format-and-VM-bytecode,-.yydebug-format-and-debugger-instructions#yydebug-file-format))
* GameMaker関連のすべてのデータファイルの自動ファイル関連付け。これはツールの初回起動時に登録されますが、実行ファイルの横に「dna.txt」ファイルを配置することで無効にすることもできます。

# スクリーンショット

UTMTで出来ることのスクリーンショットをいくつかお見せします！:

## [RIBBIT - The Deltarune Mod](https://gamejolt.com/games/ribbitmod/671888)
<img src="images/ribbit-dr.png" alt="RIBBIT" width="640" height="480"/>

# 拡張機能

UndertaleModToolには、機能を拡張するC#スクリプトがいくつか付属しています。
詳細は、[SCRIPTS.md](https://github.com/UnderminersTeam/UndertaleModTool/blob/master/SCRIPTS.md)ファイルを参照してください。

# 貢献

全ての貢献を歓迎します！ もしバグを見つけたら、(データファイルが読み込まれない等) [Issuesページ](https://github.com/UnderminersTeam/UndertaleModTool/issues)でご報告ください。Pull requestも歓迎します！ 現在取り組む必要があることは以下の通りです:

* プロファイルシステムを、より優れた、ソース管理に適したプロジェクトシステムにアップグレードします。
* より幅広いGameMakerバージョン（特に最新バージョン）のサポートを継続的に改善します。
* 主に[Underanalyzer](https://github.com/UnderminersTeam/Underanalyzer)上で、GMLコンパイラとデコンパイラのさらなる作業を行います。
* ライブラリを整理するための構造変更を行います（段階的な作業です）。
* 最終的には、可能であればGUIをクロスプラットフォーム化し、全体的に改善します。
* 全体的なユーザビリティの向上、バグ修正など。

# ビルド手順

自分でリポジトリをビルドする場合、`.NET Core 8 SDK`以降が必要です。

以下のプロジェクトがビルドできます:
- `UndertaleModLib`: 他すべてのプロジェクトで使用される重要なライブラリ。
- `UndertaleModCli`: GameMakerデータファイルを操作し、スクリプトを適用するためのCLI。現時点で出来ることはとても少ないです。
- `UndertaleModTool`: GameMakerデータファイルを操作するためのメインのGUI。**ビルドにWindowsが必須です。**

#### IDEでビルドする
- `UndertaleModTool.sln`をIDE(Visual Studio, JetBrains Rider, VSCodeなど)で開く
- ビルドしたいプロジェクトを選択
- ビルドする

#### コマンドラインでビルドする
- ターミナルで`UndertaleModTool.sln`のあるディレクトリに移動
- `dotnet publish <プロジェクト名>`を実行します。上記3つのプロジェクトがビルドできます。
`--no-self-contained` や `-c release` などの引数を指定することもできます。引数のリストは、[Microsoftのドキュメント](https://docs.microsoft.com/dotnet/core/tools/dotnet-publish) を参照してください。

# GameMakerデータファイル形式

このツールを作るときに私が行ったファイルと命令フォーマットの調査に興味がありますか？
詳細とドキュメントは、[Wiki](https://github.com/UnderminersTeam/UndertaleModTool/wiki)をご覧ください。

# スペシャルサンクス

Undertaleの解凍と逆コンパイルについてこれまで調査してくださった皆様に感謝を申し上げます。本当に大きな助けになりました。

* [PoroCYon's UNDERTALE decompilation research, maintained by Tomat](https://tomat.dev/undertale)
* [Donkeybonks's GameMaker data.win Bytecode research](https://web.archive.org/web/20191126144953if_/https://github.com/donkeybonks/acolyte/wiki/Bytecode)
* [PoroCYon's Altar.NET](https://github.com/PoroCYon/Altar.NET)
* [WarlockD's GMdsam](https://github.com/WarlockD/GMdsam)

および他全ての貢献してくれた方:
<p align="center">
  <a href="https://github.com/UnderminersTeam/UndertaleModTool/graphs/contributors">
    <img src="https://contrib.rocks/image?repo=UnderminersTeam/UndertaleModTool" />
  </a>
</p>

そしてもちろん、ゲームを制作したトビー・フォックスさんおよびUndertaleチームに感謝を申し上げます！