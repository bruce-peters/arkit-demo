# Generated Makefile - compiles IL2CPP C++ to libGameAssembly.a
# Uses Xcode's clang++ directly, bypassing Unity's broken IL2CPP toolchain

XCODE_DEVELOPER := $(shell xcode-select -p)
SDK_PATH := $(shell xcrun --sdk iphoneos --show-sdk-path)
CLANG := $(XCODE_DEVELOPER)/Toolchains/XcodeDefault.xctoolchain/usr/bin/clang++

SRC_DIR := Il2CppOutputProject/Source/il2cppOutput
OBJ_DIR := Il2CppTempDirArtifacts/Release/objs
TARGET := $(CONFIGURATION_BUILD_DIR)/libGameAssembly.a

LIB_DIR := Libraries
IL2CPP_DIR := Il2CppOutputProject/IL2CPP

SOURCES := $(wildcard $(SRC_DIR)/*.cpp)
OBJECTS := $(patsubst $(SRC_DIR)/%.cpp,$(OBJ_DIR)/%.o,$(SOURCES))

CFLAGS := -arch arm64 -isysroot $(SDK_PATH) -miphoneos-version-min=12.0 \
	-std=c++17 -stdlib=libc++ -fno-exceptions -fno-rtti \
	-O2 -DNDEBUG -DIL2CPP_ENABLE_WRITE_BARRIERS=1 \
	-I$(SRC_DIR) \
	-I$(IL2CPP_DIR)/libil2cpp \
	-I$(IL2CPP_DIR)/libil2cpp/pch \
	-I$(IL2CPP_DIR)/libil2cpp/codegen \
	-I$(IL2CPP_DIR)/libil2cpp/os \
	-I$(IL2CPP_DIR)/libil2cpp/os/Posix \
	-I$(IL2CPP_DIR)/libil2cpp/os/c-api \
	-I$(IL2CPP_DIR)/libil2cpp/utils \
	-I$(IL2CPP_DIR)/libil2cpp/vm \
	-I$(IL2CPP_DIR)/libil2cpp/vm-utils \
	-I$(IL2CPP_DIR)/libil2cpp/metadata \
	-I$(IL2CPP_DIR)/libil2cpp/gc \
	-I$(IL2CPP_DIR)/libil2cpp/mono \
	-I$(IL2CPP_DIR)/libil2cpp/icalls \
	-I$(IL2CPP_DIR)/libil2cpp/icalls/mscorlib/System \
	-I$(IL2CPP_DIR)/libil2cpp/icalls/System \
	-I$(IL2CPP_DIR)/libmono \
	-I$(IL2CPP_DIR)/external \
	-I$(LIB_DIR) \
	-I$(LIB_DIR)/baselib/Include \
	-I$(LIB_DIR)/baselib/Include/C \
	-I$(LIB_DIR)/baselib/Include/Cpp \
	-I$(LIB_DIR)/baselib/Include/Cpp/Internal

.PHONY: all clean

all: $(TARGET)

$(OBJ_DIR):
	mkdir -p $(OBJ_DIR)

$(TARGET): $(OBJ_DIR) $(OBJECTS)
	ar rcs $@ $(OBJECTS)
	@echo "Built $@ ($(words $(OBJECTS)) objects)"

$(OBJ_DIR)/%.o: $(SRC_DIR)/%.cpp
	$(CLANG) $(CFLAGS) -c $< -o $@