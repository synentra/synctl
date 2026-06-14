#!/usr/bin/env bash

# SynCtl location
: ${SYNCTL_INSTALL_DIR:="/usr/local/bin"}

# sudo is required to copy binary to SYNCTL_INSTALL_DIR for linux
: ${USE_SUDO:="false"}

# Http request synctl
HTTP_REQUEST_SYNCTL=curl

# GitHub Organization and repo name to download release
GITHUB_ORG=synentra
GITHUB_REPO=synctl

# SynCtl filename
SYNCTL_FILENAME=synctl

SYNCTL_FILE="${SYNCTL_INSTALL_DIR}/${SYNCTL_FILENAME}"

get_system_info() {
    ARCH=$(uname -m)
    case $ARCH in
        armv7*) ARCH="arm";;
        aarch64) ARCH="arm64";;
        x86_64) ARCH="x64";;
        *) echo "Unsupported architecture: $ARCH"; exit 1;;
    esac

    OS=$(uname | tr '[:upper:]' '[:lower:]')

    # Most linux distro needs root permission to copy the file to /usr/local/bin
    if [[ "$OS" == "linux" || "$OS" == "darwin" ]] && [[ "$SYNCTL_INSTALL_DIR" == "/usr/local/bin" ]]; then
        USE_SUDO="true"
    fi
    return
}

verify_supported() {
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
        if is_release_available "$releaseTag"; then
            return
        else
            echo "The darwin_arm64 arch has no native binary for this version of SynCtl, however you can use the amd64 version so long as you have rosetta installed"
            echo "Use 'softwareupdate --install-rosetta' to install rosetta if you don't already have it"
            ARCH="x64"
            return
        fi
    fi

    echo "No prebuilt binary for ${current_osarch}"
    exit 1
}

run_as_root() {
    local cmd="$*"

    if [[ $EUID -ne 0 && $USE_SUDO = "true" ]]; then
        cmd="sudo $cmd"
    fi

    $cmd || {
        echo "Please visit https://github.com/synentra/synctl for instructions on how to install without sudo."
        exit 1
    }
    return
}

check_http_request_synctl() {
    if type "curl" > /dev/null; then
        HTTP_REQUEST_SYNCTL=curl
    elif type "wget" > /dev/null; then
        HTTP_REQUEST_SYNCTL=wget
    else
        echo "Either curl or wget is required"
        exit 1
    fi
    return
}

check_existing_synctl() {
    if [[ -f "$SYNCTL_FILE" ]]; then
        echo -e "\nSynCtl is detected:"
        "$SYNCTL_FILE" --version
        echo -e "Reinstalling SynCtl...\n"
    else
        echo -e "Installing SynCtl...\n"
    fi
    return
}

get_latest_release() {
    local synctl_release_url="https://api.github.com/repos/${GITHUB_ORG}/${GITHUB_REPO}/releases"
    local latest_release=""

    if [[ "$HTTP_REQUEST_SYNCTL" == "curl" ]]; then
        latest_release=$(curl -s --proto "=https" "$synctl_release_url" | grep \"tag_name\" | grep -v rc | awk 'NR==1{print $2}' |  sed -n 's/\"\(.*\)\",/\1/p')
    else
        latest_release=$(wget -q --max-redirect=0 --https-only --header="Accept: application/json" -O - "$synctl_release_url" | grep \"tag_name\" | grep -v rc | awk 'NR==1{print $2}' |  sed -n 's/\"\(.*\)\",/\1/p')
    fi

    if [[ -z "$latest_release" ]]; then
        echo "Failed to get latest SynCtl release tag from GitHub API"
        exit 1
    fi
    ret_val=$latest_release
    return
}

download_file() {
    LATEST_RELEASE_TAG=$1

    SYNCTL_ARTIFACT="${SYNCTL_FILENAME}-${OS}-${ARCH}.tar.gz"
    DOWNLOAD_BASE="https://github.com/${GITHUB_ORG}/${GITHUB_REPO}/releases/download"
    DOWNLOAD_URL="${DOWNLOAD_BASE}/${LATEST_RELEASE_TAG}/${SYNCTL_ARTIFACT}"

    # Create the temp directory
    SYNCTL_TMP_ROOT=$(mktemp -d "${TMPDIR:-/tmp}/synctl-install-XXXXXX")
    ARTIFACT_TMP_FILE="$SYNCTL_TMP_ROOT/$SYNCTL_ARTIFACT"

    echo "Downloading $DOWNLOAD_URL ..."
    if [[ "$HTTP_REQUEST_SYNCTL" == "curl" ]]; then
        curl -SsL --proto =https "$DOWNLOAD_URL" -o "$ARTIFACT_TMP_FILE"
    else
        wget -q --max-redirect=0 --https-only -O "$ARTIFACT_TMP_FILE" "$DOWNLOAD_URL"
    fi

    if [[ ! -f "$ARTIFACT_TMP_FILE" ]]; then
        echo "failed to download $DOWNLOAD_URL ..."
        exit 1
    fi
    return
}

is_release_available() {
    LATEST_RELEASE_TAG=$1

    SYNCTL_ARTIFACT="${SYNCTL_FILENAME}-${OS}-${ARCH}.tar.gz"
    DOWNLOAD_BASE="https://github.com/${GITHUB_ORG}/${GITHUB_REPO}/releases/download"
    DOWNLOAD_URL="${DOWNLOAD_BASE}/${LATEST_RELEASE_TAG}/${SYNCTL_ARTIFACT}"

    if [[ "$HTTP_REQUEST_SYNCTL" == "curl" ]]; then
        httpstatus=$(curl -sSLI --proto "=https" -o /dev/null -w "%{http_code}" "$DOWNLOAD_URL")
        if [[ "$httpstatus" == "200" ]]; then
            return 0
        fi
    else
        wget -q --max-redirect=0 --https-only --spider "$DOWNLOAD_URL"
        exitstatus=$?
        if [[ $exitstatus -eq 0 ]]; then
            return 0
        fi
    fi
    return 1
}

install_file() {
    tar xf "$ARTIFACT_TMP_FILE" -C "$SYNCTL_TMP_ROOT"
    local tmp_root_synctl="$SYNCTL_TMP_ROOT/$SYNCTL_FILENAME"

    if [[ ! -f "$tmp_root_synctl" ]]; then
        echo "Failed to unpack SynCtl executable."
        exit 1
    fi

    if [[ -f "$SYNCTL_FILE" ]]; then
        run_as_root rm "$SYNCTL_FILE"
    fi
    chmod +x "$tmp_root_synctl"
    mkdir -p "$SYNCTL_INSTALL_DIR"
    run_as_root cp "$tmp_root_synctl" "$SYNCTL_INSTALL_DIR"

    if [[ -f "$SYNCTL_FILE" ]]; then
        echo "$SYNCTL_FILENAME installed into $SYNCTL_INSTALL_DIR successfully."

        "$SYNCTL_FILE" --version
    else 
        echo "Failed to install $SYNCTL_FILENAME"
        exit 1
    fi
    return
}

fail_trap() {
    result=$?
    if [[ "$result" != "0" ]]; then
        echo "Failed to install SynCtl"
        echo "For support, go to https://github.com/synentra/synctl"
    fi
    cleanup
    exit $result
    return
}

cleanup() {
    if [[ -d "${SYNCTL_TMP_ROOT:-}" ]]; then
        rm -rf "$SYNCTL_TMP_ROOT"
    fi
    return
}

install_completed() {
    echo -e "\nTo get started with SynCtl, please visit https://github.com/synentra/synctl"
    return
}

# -----------------------------------------------------------------------------
# main
# -----------------------------------------------------------------------------
trap "fail_trap" EXIT

get_system_info
check_http_request_synctl

if [[ -z "$1" ]]; then
    echo "Getting the latest SynCtl..."
    get_latest_release
else
    ret_val=v$1
fi

verify_supported $ret_val
check_existing_synctl

echo "Installing $ret_val SynCtl..."

download_file $ret_val
install_file
cleanup

install_completed