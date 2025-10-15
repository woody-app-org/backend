package com.woody.backend.domain.dto;

import com.woody.backend.infraestructure.entity.Hello;

public record HelloDto(String message) {
    public static HelloDto fromModel(Hello h){
        return new HelloDto(
            h.getHello()
        );
    }
}
