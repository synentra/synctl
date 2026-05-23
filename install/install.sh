#!/usr/bin/env bash

# VectraCtl location
: ${VECTRACTL_INSTALL_DIR:="/usr/local/bin"}

# sudo is required to copy binary to VECTRACTL_INSTALL_DIR for linux
: ${USE_SUDO:="false"}

# Http request vectractl
HTTP_REQUEST_VECTRACTL=curl

# GitHub Organization and repo name to download release
GITHUB_ORG=cortexiumlabs
GITHUB_REPO=vectractl

# VectraCtl filename
VECTRACTL_FILENAME=vectractl

VECTRACTL_FILE="${VECTRACTL_INSTALL_DIR}/${VECTRACTL_FILENAME}"

getSystemInfo() {
    ARCH=$(uname -m)
    case $ARCH in
        armv7*) ARCH="arm";;
        aarch64) ARCH="arm64";;
        x86_64) ARCH="x64";;
    esac

    OS=$(uname | tr '[:upper:]' '[:lower:]')

    # Most linux distro needs root permission to copy the file to /usr/local/bin
    if [[ "$OS" == "linux" || "$OS" == "darwin" ]] && [[ "$VECTRACTL_INSTALL_DIR" == "/usr/local/bin" ]]; then
        USE_SUDO="true"
    fi
}

verifySupported() {
    releaseTag=$1
    local supported=(darwin-x64 linux-x64 linux-arm linux-arm64)
    local current_osarch="${OS}-${ARCH}"

    for osarch in "${supported[@]}"; do
        if [[ "$osarch" == "$current_osarch" ]]; then
            echo "Your system is ${OS}_${ARCH}"
            return
        fi
    done

    if [[ "$current_osarch" == "darwin-arm64" ]]; then
        if isReleaseAvailable "$releaseTag"; then
            return
        else
            echo "The darwin_arm64 arch has no native binary for this version of VectraCtl, however you can use the amd64 version so long as you have rosetta installed"
            echo "Use 'softwareupdate --install-rosetta' to install rosetta if you don't already have it"
            ARCH="x64"
            return
        fi
    fi

    echo "No prebuilt binary for ${current_osarch}"
    exit 1
}

runAsRoot() {
    local CMD="$*"

    if [[ $EUID -ne 0 && $USE_SUDO = "true" ]]; then
        CMD="sudo $CMD"
    fi

    $CMD || {
        echo "Please visit https://github.com/cortexiumlabs/vectractl for instructions on how to install without sudo."
        exit 1
    }
}

checkHttpRequestVectraCtl() {
    if type "curl" > /dev/null; then
        HTTP_REQUEST_VECTRACTL=curl
    elif type "wget" > /dev/null; then
        HTTP_REQUEST_VECTRACTL=wget
    else
        echo "Either curl or wget is required"
        exit 1
    fi
}

checkExistingVectraCtl() {
    if [[ -f "$VECTRACTL_FILE" ]]; then
        echo -e "\nVectraCtl is detected:"
        "$VECTRACTL_FILE" --version
        echo -e "Reinstalling VectraCtl...\n"
    else
        echo -e "Installing VectraCtl...\n"
    fi
}

getLatestRelease() {
    local vectrActlReleaseUrl="https://api.github.com/repos/${GITHUB_ORG}/${GITHUB_REPO}/releases"
    local latest_release=""

    if [[ "$HTTP_REQUEST_VECTRACTL" == "curl" ]]; then
        latest_release=$(curl -s "$vectrActlReleaseUrl" | grep \"tag_name\" | grep -v rc | awk 'NR==1{print $2}' |  sed -n 's/\"\(.*\)\",/\1/p')
    else
        latest_release=$(wget -q --header="Accept: application/json" -O - "$vectrActlReleaseUrl" | grep \"tag_name\" | grep -v rc | awk 'NR==1{print $2}' |  sed -n 's/\"\(.*\)\",/\1/p')
    fi

    if [[ -z "$latest_release" ]]; then
        echo "Failed to get latest VectraCtl release tag from GitHub API"
        exit 1
    fi
    ret_val=$latest_release
}

downloadFile() {
    LATEST_RELEASE_TAG=$1

    VECTRACTL_ARTIFACT="${VECTRACTL_FILENAME}-${OS}-${ARCH}.tar.gz"
    DOWNLOAD_BASE="https://github.com/${GITHUB_ORG}/${GITHUB_REPO}/releases/download"
    DOWNLOAD_URL="${DOWNLOAD_BASE}/${LATEST_RELEASE_TAG}/${VECTRACTL_ARTIFACT}"

    # Create the temp directory
    VECTRACTL_TMP_ROOT=$(mktemp -d "${TMPDIR:-/tmp}/vectractl-install-XXXXXX")
    ARTIFACT_TMP_FILE="$VECTRACTL_TMP_ROOT/$VECTRACTL_ARTIFACT"

    echo "Downloading $DOWNLOAD_URL ..."
    if [[ "$HTTP_REQUEST_VECTRACTL" == "curl" ]]; then
        curl -SsL "$DOWNLOAD_URL" -o "$ARTIFACT_TMP_FILE"
    else
        wget -q -O "$ARTIFACT_TMP_FILE" "$DOWNLOAD_URL"
    fi

    if [[ ! -f "$ARTIFACT_TMP_FILE" ]]; then
        echo "failed to download $DOWNLOAD_URL ..."
        exit 1
    fi
}

isReleaseAvailable() {
    LATEST_RELEASE_TAG=$1

    VECTRACTL_ARTIFACT="${VECTRACTL_FILENAME}-${OS}-${ARCH}.tar.gz"
    DOWNLOAD_BASE="https://github.com/${GITHUB_ORG}/${GITHUB_REPO}/releases/download"
    DOWNLOAD_URL="${DOWNLOAD_BASE}/${LATEST_RELEASE_TAG}/${VECTRACTL_ARTIFACT}"

    if [[ "$HTTP_REQUEST_VECTRACTL" == "curl" ]]; then
        httpstatus=$(curl -sSLI -o /dev/null -w "%{http_code}" "$DOWNLOAD_URL")
        if [[ "$httpstatus" == "200" ]]; then
            return 0
        fi
    else
        wget -q --spider "$DOWNLOAD_URL"
        exitstatus=$?
        if [[ $exitstatus -eq 0 ]]; then
            return 0
        fi
    fi
    return 1
}

installFile() {
    tar xf "$ARTIFACT_TMP_FILE" -C "$VECTRACTL_TMP_ROOT"
    local tmp_root_vectractl="$VECTRACTL_TMP_ROOT/$VECTRACTL_FILENAME"

    if [[ ! -f "$tmp_root_vectractl" ]]; then
        echo "Failed to unpack VectraCtl executable."
        exit 1
    fi

    if [[ -f "$VECTRACTL_FILE" ]]; then
        runAsRoot rm "$VECTRACTL_FILE"
    fi
    chmod +x "$tmp_root_vectractl"
    mkdir -p "$VECTRACTL_INSTALL_DIR"
    runAsRoot cp "$tmp_root_vectractl" "$VECTRACTL_INSTALL_DIR"

    if [[ -f "$VECTRACTL_FILE" ]]; then
        echo "$VECTRACTL_FILENAME installed into $VECTRACTL_INSTALL_DIR successfully."

        "$VECTRACTL_FILE" --version
    else 
        echo "Failed to install $VECTRACTL_FILENAME"
        exit 1
    fi
}

fail_trap() {
    result=$?
    if [[ "$result" != "0" ]]; then
        echo "Failed to install VectraCtl"
        echo "For support, go to https://github.com/cortexiumlabs/vectractl"
    fi
    cleanup
    exit $result
}

cleanup() {
    if [[ -d "${VECTRACTL_TMP_ROOT:-}" ]]; then
        rm -rf "$VECTRACTL_TMP_ROOT"
    fi
}

installCompleted() {
    echo -e "\nTo get started with VectraCtl, please visit https://github.com/cortexiumlabs/vectractl"
}

# -----------------------------------------------------------------------------
# main
# -----------------------------------------------------------------------------
trap "fail_trap" EXIT

getSystemInfo
checkHttpRequestVectraCtl

if [[ -z "$1" ]]; then
    echo "Getting the latest VectraCtl..."
    getLatestRelease
else
    ret_val=v$1
fi

verifySupported $ret_val
checkExistingVectraCtl

echo "Installing $ret_val VectraCtl..."

downloadFile $ret_val
installFile
cleanup

installCompleted