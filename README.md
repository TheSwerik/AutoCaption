#

## Prerequisites

### Install Whisper

1. Install Python >=3.10 <3.14
2. Install Whisper

```bash
  pip install git+https://github.com/openai/whisper.git
  choco install ffmpeg
  pip install setuptools-rust
  pip uninstall torch torchaudio torchvision -y
  pip install torch torchvision torchaudio --index-url https://download.pytorch.org/whl/cu121
```