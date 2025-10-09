"""
NetherGate Python SDK 安装脚本
"""

from setuptools import setup, find_packages

with open("README.md", "r", encoding="utf-8") as f:
    long_description = f.read()

setup(
    name="nethergate-python",
    version="1.0.0",
    author="NetherGate Team",
    author_email="your-email@example.com",
    description="Python SDK for NetherGate plugin development",
    long_description=long_description,
    long_description_content_type="text/markdown",
    url="https://github.com/your-org/NetherGate",
    packages=find_packages(),
    classifiers=[
        "Development Status :: 4 - Beta",
        "Intended Audience :: Developers",
        "Topic :: Software Development :: Libraries :: Python Modules",
        "License :: OSI Approved :: MIT License",
        "Programming Language :: Python :: 3",
        "Programming Language :: Python :: 3.8",
        "Programming Language :: Python :: 3.9",
        "Programming Language :: Python :: 3.10",
        "Programming Language :: Python :: 3.11",
        "Programming Language :: Python :: 3.12",
    ],
    python_requires=">=3.8",
    install_requires=[
        # 这里可以添加 Python SDK 的依赖
    ],
    extras_require={
        "dev": [
            "pytest>=7.0.0",
            "black>=22.0.0",
            "flake8>=4.0.0",
            "mypy>=0.950",
        ]
    },
    project_urls={
        "Bug Reports": "https://github.com/your-org/NetherGate/issues",
        "Source": "https://github.com/your-org/NetherGate",
        "Documentation": "https://nethergate.readthedocs.io",
    },
)

